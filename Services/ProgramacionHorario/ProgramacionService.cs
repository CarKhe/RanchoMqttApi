
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace RanchoMqttApi;

public class ProgramacionService : IProgramacionService
{
    private readonly DBContext _db;
    private readonly IReleService _rele;
    private readonly IHubContext<RelesHub> _hub;
    private readonly RiegoOptions _opciones;
    public ProgramacionService(DBContext db, IReleService rele, IHubContext<RelesHub> hub
        ,RiegoOptions opciones)
    {
        _db = db;
        _rele = rele;
        _hub = hub;
        _opciones = opciones;
    }
    public async Task<(bool exito, string mensaje, int? id)> CrearAsync(CrearProgramacionDto dto)
    {
        var (valido, error) = await ValidarAsync(dto);
        if (!valido) return (false, error, null);

        var programacion = new ProgramacionRiego
        {
            Nombre = dto.Nombre.Trim(),
            horaInicio = dto.HoraInicio,
            horaFin = dto.HoraFin,
            diasSemana = dto.DiasSemana,
            modoEjecucion = dto.ModoEjecucion,
            habilitada = true,
            fechaCreacion = DateTime.UtcNow      // Utc obligatorio, si no truena Npgsql
        };

        var orden = 1;
        foreach (var r in dto.Reles.OrderBy(r => r.Orden))
        {
            programacion.reles.Add(new ProgramacionRele
            {
                idRele = r.IdRele,
                duracionMinutos = r.DuracionMinutos,
                orden = orden++          // renumera 1..N, ignora huecos del cliente
            });
        }

        _db.ProgramacionesRiego.Add(programacion);
        await _db.SaveChangesAsync();

        return (true, "Programación creada", programacion.idProgramacion);
    }

    public async Task<List<ProgramacionDto>> ObtenerTodasAsync()
    {
        var programaciones = await _db.ProgramacionesRiego
            .Include(p => p.reles).ThenInclude(pr => pr.rele).ThenInclude(r => r!.tipoRele)
            .OrderBy(p => p.horaInicio)
            .AsNoTracking()
            .ToListAsync();

        return programaciones.Select(p => new ProgramacionDto(
            p.idProgramacion, p.Nombre, p.habilitada,
            p.horaInicio, p.horaFin, p.diasSemana,
            p.modoEjecucion.ToString(),
            p.reles.OrderBy(r => r.orden).Select(r => new ProgramacionReleDto(
                r.idRele, r.rele!.Nombre, r.rele!.tipoRele!.nombreRele,
                r.duracionMinutos, r.orden
            )).ToList()
        )).ToList();
    }

    public async Task<(bool exito, string mensaje)> CancelarHoyAsync(int idProgramacion, CancellationToken ct)
    {
        var corrida = await _db.EjecucionesProgramacion
            .Include(e => e.detalles)
                .ThenInclude(d => d.rele)
                    .ThenInclude(r => r!.tipoRele)
            .OrderByDescending(e => e.fecha)
            .FirstOrDefaultAsync(e => e.idProgramacion == idProgramacion
                                && e.estado == EstadosEjecucion.EnCurso, ct);

        if (corrida is null)
            return (false, "Esa programación no tiene una corrida activa ahorita");

        var activos = corrida.detalles
            .Where(d => d.estado == EstadoDetalle.EnCurso)
            .ToList();

        foreach (var d in corrida.detalles.Where(d => d.estado == EstadoDetalle.EnCurso
                                                || d.estado == EstadoDetalle.Pendiente))
        {
            d.estado = EstadoDetalle.CanceladaPorUsuario;
            if (d.inicioReal is not null) d.finReal = DateTime.UtcNow;
        }

        corrida.estado = EstadosEjecucion.CanceladaPorUsuario;
        corrida.finReal = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);          // <-- marcar PRIMERO

        foreach (var d in activos)               // <-- apagar DESPUÉS
            await _rele.CambiarEstadoAsync(
                d.rele!.tipoRele!.nombreRele, d.idRele, false, OrigenComando.Programado);

        await _hub.Clients.All.SendAsync(HubMethods.EjecucionActualizada, idProgramacion, ct);

        return (true, $"Programación cancelada por hoy, {activos.Count} relé(s) apagado(s)");
    }

    public async Task<(bool exito, string mensaje)> ActualizarAsync(int id, CrearProgramacionDto dto)
    {
        var programacion = await _db.ProgramacionesRiego
            .Include(p => p.reles)
            .FirstOrDefaultAsync(p => p.idProgramacion == id);

        if (programacion is null) return (false, "La programación no existe");

        var (valido, error) = await ValidarAsync(dto);
        if (!valido) return (false, error);

        programacion.Nombre = dto.Nombre.Trim();
        programacion.horaInicio = dto.HoraInicio;
        programacion.horaFin = dto.HoraFin;
        programacion.diasSemana = dto.DiasSemana;
        programacion.modoEjecucion = dto.ModoEjecucion;

        _db.ProgramacionReles.RemoveRange(programacion.reles);   // reemplazo completo

        var orden = 1;
        foreach (var r in dto.Reles.OrderBy(r => r.Orden))
        {
            programacion.reles.Add(new ProgramacionRele
            {
                idRele = r.IdRele,
                duracionMinutos = r.DuracionMinutos,
                orden = orden++
            });
        }

        await _db.SaveChangesAsync();
        return (true, "Programación actualizada");
    }

    public async Task<(bool exito, string mensaje)> EliminarAsync(int id)
    {
        var programacion = await _db.ProgramacionesRiego
            .FirstOrDefaultAsync(p => p.idProgramacion == id);

        if (programacion is null) return (false, "La programación no existe");

        var tieneHistorial = await _db.EjecucionesProgramacion
            .AnyAsync(e => e.idProgramacion == id);

        if (tieneHistorial)
        {
            programacion.habilitada = false;
            await _db.SaveChangesAsync();
            return (true, "Tiene historial de riegos: se deshabilitó en vez de borrarse");
        }

        _db.ProgramacionesRiego.Remove(programacion);
        await _db.SaveChangesAsync();
        return (true, "Programación eliminada");
    }
    public async Task<(bool exito, string mensaje)> CambiarHabilitadaAsync(int id, bool valor)
    {
        var programacion = await _db.ProgramacionesRiego
            .FirstOrDefaultAsync(p => p.idProgramacion == id);

        if (programacion is null) return (false, "La programación no existe");

        programacion.habilitada = valor;
        await _db.SaveChangesAsync();

        return (true, valor ? "Programación habilitada" : "Programación deshabilitada");
    }


    public async Task<List<CorridaDto>> ObtenerCorridasDeHoyAsync(CancellationToken ct)
    {
        var zona = TimeZoneInfo.FindSystemTimeZoneById(_opciones.ZonaHoraria);
        var hoy = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zona).DateTime);

        var corridas = await _db.EjecucionesProgramacion
            .Include(e => e.programacion)
            .Include(e => e.detalles)
                .ThenInclude(d => d.rele)
                    .ThenInclude(r => r!.tipoRele)
            .Where(e => e.fecha == hoy)
            .AsNoTracking()
            .ToListAsync(ct);

        return corridas.Select(e => new CorridaDto(
            e.idEjecucion,
            e.idProgramacion,
            e.programacion!.Nombre,
            e.fecha,
            e.estado.ToString(),
            e.programacion.horaInicio,
            e.programacion.horaFin,
            e.inicioReal,
            e.finReal,
            e.detalles.OrderBy(d => d.orden).Select(d => new CorridaReleDto(
                d.idRele,
                d.rele!.Nombre,
                d.rele!.tipoRele!.nombreRele,
                d.orden,
                d.duracionMinutos,
                d.estado.ToString(),
                d.inicioReal,
                d.finPrevisto,
                d.finReal
            )).ToList()
        )).ToList();
    }

    private async Task<(bool valido, string error)> ValidarAsync(CrearProgramacionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return (false, "El nombre es obligatorio");

        if (dto.HoraFin <= dto.HoraInicio)
            return (false, "La hora de fin debe ser posterior a la de inicio");

        if (dto.DiasSemana is < 1 or > 127)
            return (false, "Debes seleccionar al menos un día de la semana");

        if (dto.Reles is null || dto.Reles.Count == 0)
            return (false, "Debes incluir al menos un relevador");

        if (dto.Reles.Any(r => r.DuracionMinutos <= 0))
            return (false, "La duración debe ser mayor a cero");

        if (dto.Reles.Select(r => r.IdRele).Distinct().Count() != dto.Reles.Count)
            return (false, "Hay relevadores repetidos");

        var idsPedidos = dto.Reles.Select(r => r.IdRele).ToList();
        var idsExistentes = await _db.Rele
            .Where(r => idsPedidos.Contains(r.idRele))
            .Select(r => r.idRele)
            .ToListAsync();

        var faltantes = idsPedidos.Except(idsExistentes).ToList();
        if (faltantes.Count > 0)
            return (false, $"Estos relevadores no existen: {string.Join(", ", faltantes)}");

        // aquí es donde importa el modo del que hablábamos
        var minutosVentana = (dto.HoraFin - dto.HoraInicio).TotalMinutes;
        var minutosNecesarios = dto.ModoEjecucion == ModoEjecucion.Secuencial
            ? dto.Reles.Sum(r => r.DuracionMinutos)
            : dto.Reles.Max(r => r.DuracionMinutos);

        if (minutosNecesarios > minutosVentana)
            return (false, $"La ventana es de {minutosVentana:0} min y necesitas {minutosNecesarios} min");

        return (true, "");
    }


}
