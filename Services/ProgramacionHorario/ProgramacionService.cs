
using Microsoft.EntityFrameworkCore;

namespace RanchoMqttApi;

public class ProgramacionService : IProgramacionService
{
    private readonly DBContext _db;
    public ProgramacionService(DBContext db)
    {
        _db = db;
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
