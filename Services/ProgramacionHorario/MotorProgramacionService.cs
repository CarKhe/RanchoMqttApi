using Microsoft.EntityFrameworkCore;

namespace RanchoMqttApi;

public class MotorProgramacionService : IMotorProgramacionService
{
    private readonly DBContext _db;
    private readonly IReleService _rele;
    private readonly RiegoOptions _opciones;
    private readonly ILogger<MotorProgramacionService> _logger;
    private readonly TimeZoneInfo _zona;

    public MotorProgramacionService(
        DBContext db,
        IReleService rele,
        RiegoOptions opciones,
        ILogger<MotorProgramacionService> logger)
    {
        _db = db;
        _rele = rele;
        _opciones = opciones;
        _logger = logger;
        _zona = TimeZoneInfo.FindSystemTimeZoneById(opciones.ZonaHoraria);
    }

    public async Task TickAsync(CancellationToken ct)
    {
        var ahora = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _zona);

        await MaterializarOcurrenciasAsync(ahora, ct);
        await ArrancarEjecucionesAsync(ahora, ct);
        await AvanzarRelesAsync(ahora, ct);
        await CerrarVentanasAsync(ahora, ct);
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
                estado = EstadosEjecucion.Pendiente
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
            var inicio = EnLocal(hoy, programacion.horaInicio);
            var fin = EnLocal(hoy, programacion.horaFin);

            // todavía no le toca
            if (ahora < inicio)
            {
                _logger.LogDebug("'{Nombre}' aún no arranca: son las {Ahora}, empieza {Inicio}",
                    programacion.Nombre, ahora.ToString("HH:mm:ss"), programacion.horaInicio);
                continue;
            }

            // la ventana ya cerró completa: la API estuvo apagada todo ese rato
            if (ahora >= fin)
            {
                corrida.estado = EstadosEjecucion.Completada;
                foreach (var d in corrida.detalles)
                    d.estado = EstadoDetalle.Omitida;

                _logger.LogWarning("Ventana perdida: '{Nombre}' cerró a las {Fin}, no se riega",
                    programacion.Nombre, programacion.horaFin);
                continue;
            }

            corrida.estado = EstadosEjecucion.EnCurso;
            corrida.inicioReal = ahora.UtcDateTime;

            var aEncender = programacion.modoEjecucion == ModoEjecucion.Simultaneo
                ? corrida.detalles
                    .Where(d => d.estado == EstadoDetalle.Pendiente)
                    .ToList()
                : corrida.detalles
                    .Where(d => d.estado == EstadoDetalle.Pendiente)
                    .OrderBy(d => d.orden)
                    .Take(1)
                    .ToList();

            _logger.LogInformation("Arrancando '{Nombre}' en modo {Modo}, {N} relé(s)",
                programacion.Nombre, programacion.modoEjecucion, aEncender.Count);

            foreach (var d in aEncender)
                EncenderDetalle(d, ahora, fin);
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
            var finVentana = EnLocal(hoy, programacion.horaFin);

            // 3a. apagar los que ya cumplieron su tiempo
            var vencidos = corrida.detalles
                .Where(d => d.estado == EstadoDetalle.EnCurso
                        && d.finPrevisto is not null
                        && d.finPrevisto <= ahora.UtcDateTime)
                .ToList();

            foreach (var d in vencidos)
                ApagarDetalle(d, ahora, EstadoDetalle.Completada);

            // 3b. en secuencial, encender el siguiente
            if (programacion.modoEjecucion != ModoEjecucion.Secuencial) continue;
            if (ahora >= finVentana) continue;

            if (corrida.detalles.Any(d => d.estado == EstadoDetalle.EnCurso)) continue;

            var siguiente = corrida.detalles
                .Where(d => d.estado == EstadoDetalle.Pendiente)
                .OrderBy(d => d.orden)
                .FirstOrDefault();

            if (siguiente is null) continue;

            EncenderDetalle(siguiente, ahora, finVentana);
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
            var ventanaCerrada = ahora >= EnLocal(hoy, programacion.horaFin);

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
                    ApagarDetalle(d, ahora, EstadoDetalle.Completada);

                foreach (var d in pendientes)
                    d.estado = EstadoDetalle.Omitida;

                if (pendientes.Count > 0)
                    _logger.LogWarning("'{Nombre}': {N} relé(s) sin turno antes de las {Fin}",
                        programacion.Nombre, pendientes.Count, programacion.horaFin);
            }

            corrida.estado = EstadosEjecucion.Completada;
            corrida.finReal = ahora.UtcDateTime;

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
    private void EncenderDetalle(EjecucionReleDetalle detalle,
                                DateTimeOffset ahora,
                                DateTimeOffset finVentana)
    {
        var finPrevisto = ahora.AddMinutes(detalle.duracionMinutos);

        // si no cabe en la ventana, se recorta
        if (finPrevisto > finVentana) finPrevisto = finVentana;

        detalle.estado = EstadoDetalle.EnCurso;
        detalle.inicioReal = ahora.UtcDateTime;
        detalle.finPrevisto = finPrevisto.UtcDateTime;

        if (_opciones.ModoSimulacion)
        {
            _logger.LogInformation("[SIM] ENCENDER relé {Id}, {Min} min, hasta las {Fin}",
                detalle.idRele, detalle.duracionMinutos, finPrevisto.ToString("HH:mm:ss"));
            return;
        }

        // Fase 4: aquí va la llamada real a _rele.CambiarEstadoAsync(...)
    }

    private void ApagarDetalle(EjecucionReleDetalle detalle,
                            DateTimeOffset ahora,
                            EstadoDetalle estadoFinal)
    {
        detalle.estado = estadoFinal;
        detalle.finReal = ahora.UtcDateTime;

        if (_opciones.ModoSimulacion)
        {
            _logger.LogInformation("[SIM] APAGAR relé {Id} ({Motivo})",
                detalle.idRele, estadoFinal);
            return;
        }

        // Fase 4: aquí va _rele.CambiarEstadoAsync(tipo, id, false, OrigenComando.Programado)
    }
}
