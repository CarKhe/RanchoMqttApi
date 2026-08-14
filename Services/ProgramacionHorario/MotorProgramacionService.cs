using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace RanchoMqttApi;

public class MotorProgramacionService : IMotorProgramacionService
{
    private readonly DBContext _db;
    private readonly IReleService _rele;
    private readonly RiegoOptions _opciones;
    private readonly ILogger<MotorProgramacionService> _logger;
    private readonly TimeZoneInfo _zona;
    private readonly IHubContext<RelesHub> _hub;
    private readonly HashSet<int> _tocadas = [];
    private readonly IReleCacheService _cache;

    public MotorProgramacionService(
        DBContext db,
        IReleService rele,
        RiegoOptions opciones,
        ILogger<MotorProgramacionService> logger,
        IHubContext<RelesHub> hub,
        IReleCacheService cache)
    {
        _db = db;
        _rele = rele;
        _opciones = opciones;
        _logger = logger;
        _zona = TimeZoneInfo.FindSystemTimeZoneById(opciones.ZonaHoraria);
        _hub = hub;
        _cache = cache;
    }

    public async Task TickAsync(CancellationToken ct)
    {
        var ahora = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _zona);

        await MaterializarOcurrenciasAsync(ahora, ct);
        await ArrancarEjecucionesAsync(ahora, ct);
        await AvanzarRelesAsync(ahora, ct);
        await CerrarVentanasAsync(ahora, ct);

        await NotificarCambiosAsync(ct);
    }

    public async Task ReconciliarTrasReinicioAsync(CancellationToken ct)
    {
        var ahora = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _zona);
        var hoy = DateOnly.FromDateTime(ahora.DateTime);

        var abiertas = await _db.EjecucionesProgramacion
            .Include(e => e.programacion)
            .Include(e => e.detalles)
                .ThenInclude(d => d.rele)
                    .ThenInclude(r => r!.tipoRele)
            .Where(e => e.estado == EstadosEjecucion.EnCurso)
            .ToListAsync(ct);

        if (abiertas.Count == 0)
        {
            _logger.LogInformation("Reconciliación: no había corridas abiertas");
            return;
        }

        foreach (var corrida in abiertas)
        {
            var nombre = corrida.programacion!.Nombre;

            // caso 1: quedó abierta de un día anterior
            if (corrida.fecha < hoy)
            {
                foreach (var d in corrida.detalles.Where(d => d.estado == EstadoDetalle.EnCurso))
                    await ApagarDetalleAsync(d, ahora, EstadoDetalle.Fallida, ct);

                foreach (var d in corrida.detalles.Where(d => d.estado == EstadoDetalle.Pendiente))
                    d.estado = EstadoDetalle.Omitida;

                corrida.estado = EstadosEjecucion.Fallida;
                corrida.finReal = ahora.UtcDateTime;
                _tocadas.Add(corrida.idProgramacion);

                _logger.LogWarning("Reconciliación: '{Nombre}' del {Fecha} quedó abierta, se cierra como fallida",
                    nombre, corrida.fecha);
                continue;
            }

            // caso 2: es de hoy — comparar contra lo que el ESP32 confirmó
            foreach (var d in corrida.detalles.Where(d => d.estado == EstadoDetalle.EnCurso))
            {
                var tipo = d.rele!.tipoRele!.nombreRele;
                var enCache = _cache.Obtener(tipo, d.idRele);

                if (enCache is null)
                {
                    _logger.LogWarning("Reconciliación: no sé el estado real de {Tipo}/{Id}, lo dejo como está",
                        tipo, d.idRele);
                    continue;
                }

                if (enCache.Estado != "on")
                {
                    d.estado = EstadoDetalle.Fallida;
                    d.finReal = ahora.UtcDateTime;
                    _tocadas.Add(corrida.idProgramacion);

                    _logger.LogWarning("Reconciliación: el motor creía {Tipo}/{Id} encendido, pero está '{Real}'",
                        tipo, d.idRele, enCache.Estado);
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        await NotificarCambiosAsync(ct);
    }

    private async Task MaterializarOcurrenciasAsync(DateTimeOffset ahora, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(ahora.DateTime);
        var bitHoy = 1 << (int)hoy.DayOfWeek;

        var programaciones = await _db.ProgramacionesRiego
            .Include(p => p.reles)
            .Where(p => p.habilitada && (p.diasSemana & bitHoy) != 0)
            .AsNoTracking()
            .ToListAsync(ct);

        if (programaciones.Count == 0) return;

        var yaTienenCorrida = await _db.EjecucionesProgramacion
            .Where(e => e.fecha == hoy)
            .Select(e => e.idProgramacion)
            .ToListAsync(ct);

        var faltantes = programaciones
            .Where(p => !yaTienenCorrida.Contains(p.idProgramacion))
            .ToList();

        if (faltantes.Count == 0) return;

        foreach (var p in faltantes)
        {
            var corrida = new EjecucionProgramacion
            {
                idProgramacion = p.idProgramacion,
                fecha = hoy,
                estado = EstadosEjecucion.Pendiente,
                horaInicio = p.horaInicio,
                horaFin = p.horaFin,
                modoEjecucion = p.modoEjecucion
            };

            foreach (var pr in p.reles.OrderBy(r => r.orden))
            {
                corrida.detalles.Add(new EjecucionReleDetalle
                {
                    idRele = pr.idRele,
                    orden = pr.orden,
                    duracionMinutos = pr.duracionMinutos,   // la copia
                    estado = EstadoDetalle.Pendiente
                });
            }

            _db.EjecucionesProgramacion.Add(corrida);
            _tocadas.Add(p.idProgramacion);

            _logger.LogInformation("Corrida creada: '{Nombre}' para {Fecha}, {N} relés",
                p.Nombre, hoy, corrida.detalles.Count);
        }

    await _db.SaveChangesAsync(ct);
    }

    private async Task ArrancarEjecucionesAsync(DateTimeOffset ahora, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(ahora.DateTime);

        var pendientes = await _db.EjecucionesProgramacion
            .Include(e => e.programacion)
            .Include(e => e.detalles)
                .ThenInclude(d => d.rele)
                    .ThenInclude(r => r!.tipoRele)      // lo vas a necesitar en la Fase 4
            .Where(e => e.fecha == hoy && e.estado == EstadosEjecucion.Pendiente)
            .ToListAsync(ct);

        if (pendientes.Count == 0) return;

        foreach (var corrida in pendientes)
        {
            var programacion = corrida.programacion!;
            var inicio = EnLocal(hoy, corrida.horaInicio);
            var fin = EnLocal(hoy, corrida.horaFin);

            // todavía no le toca
            if (ahora < inicio)
            {
                _logger.LogDebug("'{Nombre}' aún no arranca: son las {Ahora}, empieza {Inicio}",
                    programacion.Nombre, ahora.ToString("HH:mm:ss"), corrida.horaInicio);
                continue;
            }

            // de aquí en adelante cualquier camino cambia estado
            _tocadas.Add(corrida.idProgramacion);

            // la ventana ya cerró completa: la API estuvo apagada todo ese rato
            if (ahora >= fin)
            {
                corrida.estado = EstadosEjecucion.Completada;
                foreach (var d in corrida.detalles)
                    d.estado = EstadoDetalle.Omitida;

                _logger.LogWarning("Ventana perdida: '{Nombre}' cerró a las {Fin}, no se riega",
                    programacion.Nombre, corrida.horaFin);
                continue;
            }

            corrida.estado = EstadosEjecucion.EnCurso;
            corrida.inicioReal = ahora.UtcDateTime;

            var aEncender = corrida.modoEjecucion == ModoEjecucion.Simultaneo
                ? corrida.detalles
                    .Where(d => d.estado == EstadoDetalle.Pendiente)
                    .ToList()
                : corrida.detalles
                    .Where(d => d.estado == EstadoDetalle.Pendiente)
                    .OrderBy(d => d.orden)
                    .Take(1)
                    .ToList();

            _logger.LogInformation("Arrancando '{Nombre}' en modo {Modo}, {N} relé(s)",
                programacion.Nombre, corrida.modoEjecucion, aEncender.Count);

            foreach (var d in aEncender)
                await EncenderDetalleAsync(d, ahora, fin, ct);
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task AvanzarRelesAsync(DateTimeOffset ahora, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(ahora.DateTime);

        var enCurso = await _db.EjecucionesProgramacion
            .Include(e => e.programacion)
            .Include(e => e.detalles)
                .ThenInclude(d => d.rele)
                    .ThenInclude(r => r!.tipoRele)
            .Where(e => e.fecha == hoy && e.estado == EstadosEjecucion.EnCurso)
            .ToListAsync(ct);

        if (enCurso.Count == 0) return;

        foreach (var corrida in enCurso)
        {
            var programacion = corrida.programacion!;
            var finVentana = EnLocal(hoy, corrida.horaFin);

            // 3a. apagar los que ya cumplieron su tiempo
            var vencidos = corrida.detalles
                .Where(d => d.estado == EstadoDetalle.EnCurso
                        && d.finPrevisto is not null
                        && d.finPrevisto <= ahora.UtcDateTime)
                .ToList();

            foreach (var d in vencidos)
                await ApagarDetalleAsync(d, ahora, EstadoDetalle.Completada, ct);

            if (vencidos.Count > 0) _tocadas.Add(corrida.idProgramacion);

            // 3b. en secuencial, encender el siguiente
            if (corrida.modoEjecucion != ModoEjecucion.Secuencial) continue;
            if (ahora >= finVentana) continue;

            if (corrida.detalles.Any(d => d.estado == EstadoDetalle.EnCurso)) continue;

            var siguiente = corrida.detalles
                .Where(d => d.estado == EstadoDetalle.Pendiente)
                .OrderBy(d => d.orden)
                .FirstOrDefault();

            if (siguiente is null) continue;

            await EncenderDetalleAsync(siguiente, ahora, finVentana, ct);
            _tocadas.Add(corrida.idProgramacion);

        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task CerrarVentanasAsync(DateTimeOffset ahora, CancellationToken ct)
    {
        var hoy = DateOnly.FromDateTime(ahora.DateTime);

        var enCurso = await _db.EjecucionesProgramacion
            .Include(e => e.programacion)
            .Include(e => e.detalles)
                .ThenInclude(d => d.rele)
                    .ThenInclude(r => r!.tipoRele)
            .Where(e => e.fecha == hoy && e.estado == EstadosEjecucion.EnCurso)
            .ToListAsync(ct);

        if (enCurso.Count == 0) return;

        foreach (var corrida in enCurso)
        {
            var programacion = corrida.programacion!;
            var ventanaCerrada = ahora >= EnLocal(hoy, corrida.horaFin);

            var activos = corrida.detalles
                .Where(d => d.estado == EstadoDetalle.EnCurso).ToList();
            var pendientes = corrida.detalles
                .Where(d => d.estado == EstadoDetalle.Pendiente).ToList();

            // la ventana sigue abierta y todavía queda trabajo: no se cierra
            if (!ventanaCerrada && (activos.Count > 0 || pendientes.Count > 0))
                continue;

            if (ventanaCerrada)
            {
                foreach (var d in activos)
                    await ApagarDetalleAsync(d, ahora, EstadoDetalle.Completada, ct);

                foreach (var d in pendientes)
                    d.estado = EstadoDetalle.Omitida;

                if (pendientes.Count > 0)
                    _logger.LogWarning("'{Nombre}': {N} relé(s) sin turno antes de las {Fin}",
                        programacion.Nombre, pendientes.Count, corrida.horaFin);
            }

            corrida.estado = EstadosEjecucion.Completada;
            corrida.finReal = ahora.UtcDateTime;
            _tocadas.Add(corrida.idProgramacion);

            _logger.LogInformation("Corrida cerrada: '{Nombre}' ({Motivo})",
                programacion.Nombre,
                ventanaCerrada ? "fin de ventana" : "todos los relés terminaron");
        }

        await _db.SaveChangesAsync(ct);
    }

    // convierte "el 11 de agosto a las 15:00" en un instante real con su offset
    private DateTimeOffset EnLocal(DateOnly fecha, TimeOnly hora)
    {
        var sinZona = fecha.ToDateTime(hora);                  // Kind = Unspecified
        return new DateTimeOffset(sinZona, _zona.GetUtcOffset(sinZona));
    }
    private async Task EncenderDetalleAsync(EjecucionReleDetalle detalle,
                                            DateTimeOffset ahora,
                                            DateTimeOffset finVentana,
                                            CancellationToken ct)
    {
        var finPrevisto = ahora.AddMinutes(detalle.duracionMinutos);
        if (finPrevisto > finVentana) finPrevisto = finVentana;

        detalle.estado = EstadoDetalle.EnCurso;
        detalle.inicioReal = ahora.UtcDateTime;
        detalle.finPrevisto = finPrevisto.UtcDateTime;

        var tipo = detalle.rele!.tipoRele!.nombreRele;

        if (_opciones.ModoSimulacion)
        {
            _logger.LogInformation("[SIM] ENCENDER {Tipo}/{Id}, {Min} min, hasta las {Fin}",
                tipo, detalle.idRele, detalle.duracionMinutos, finPrevisto.ToString("HH:mm:ss"));
            return;
        }

        var (exito, mensaje) = await _rele.CambiarEstadoAsync(
            tipo, detalle.idRele, true, OrigenComando.Programado);

        if (exito)
            _logger.LogInformation("ENCENDER {Tipo}/{Id}, {Min} min, hasta las {Fin}",
                tipo, detalle.idRele, detalle.duracionMinutos, finPrevisto.ToString("HH:mm:ss"));
        else
            _logger.LogWarning("No se pudo encender {Tipo}/{Id}: {Mensaje}",
                tipo, detalle.idRele, mensaje);
    }

    private async Task ApagarDetalleAsync(EjecucionReleDetalle detalle,
                                        DateTimeOffset ahora,
                                        EstadoDetalle estadoFinal,
                                        CancellationToken ct)
    {
        detalle.estado = estadoFinal;
        detalle.finReal = ahora.UtcDateTime;

        var tipo = detalle.rele!.tipoRele!.nombreRele;

        if (_opciones.ModoSimulacion)
        {
            _logger.LogInformation("[SIM] APAGAR {Tipo}/{Id} ({Motivo})",
                tipo, detalle.idRele, estadoFinal);
            return;
        }

        var (exito, mensaje) = await _rele.CambiarEstadoAsync(
            tipo, detalle.idRele, false, OrigenComando.Programado);

        if (exito)
            _logger.LogInformation("APAGAR {Tipo}/{Id} ({Motivo})", tipo, detalle.idRele, estadoFinal);
        else
            _logger.LogWarning("No se pudo apagar {Tipo}/{Id}: {Mensaje}", tipo, detalle.idRele, mensaje);
    }

    private async Task NotificarCambiosAsync(CancellationToken ct)
    {
        foreach (var idProgramacion in _tocadas)
            await _hub.Clients.All.SendAsync(HubMethods.EjecucionActualizada, idProgramacion, ct);
    }
}
