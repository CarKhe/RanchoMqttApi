using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RanchoMqttApi;

/// <summary>
/// Historico: cambios de estado de reles y lecturas de temperatura.
/// </summary>
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class LecturasController : ControllerBase
{
    private const int LimiteMaximo = 500;

    private readonly DBContext _db;

    public LecturasController(DBContext db) => _db = db;

    /// <summary>Historial de cambios de estado de los reles, del mas reciente al mas viejo.</summary>
    [HttpGet("historial-reles")]
    public async Task<IActionResult> HistorialReles(
        [FromQuery] int? idRele,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int limite = 100)
    {
        limite = Math.Clamp(limite, 1, LimiteMaximo);

        var query = _db.HistorialEstadoReleis.AsNoTracking().AsQueryable();

        if (idRele.HasValue) query = query.Where(h => h.idRele == idRele.Value);
        if (desde.HasValue) query = query.Where(h => h.fechaHora >= desde.Value.ToUniversalTime());
        if (hasta.HasValue) query = query.Where(h => h.fechaHora <= hasta.Value.ToUniversalTime());

        var historial = await query
            .OrderByDescending(h => h.fechaHora)
            .Take(limite)
            .Select(h => new HistorialReleDto(
                h.idHistorialEstadoRelei,
                h.idRele,
                h.rele!.Nombre,
                h.estado,
                h.exito,
                h.fechaHora))
            .ToListAsync();

        return Ok(historial);
    }

    /// <summary>Lecturas de temperatura, de la mas reciente a la mas vieja.</summary>
    [HttpGet("temperaturas")]
    public async Task<IActionResult> Temperaturas(
        [FromQuery] int? idSensor,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int limite = 100)
    {
        limite = Math.Clamp(limite, 1, LimiteMaximo);

        var query = _db.LecturaTemperaturas.AsNoTracking().AsQueryable();

        if (idSensor.HasValue) query = query.Where(l => l.idSensor == idSensor.Value);
        if (desde.HasValue) query = query.Where(l => l.FechaHora >= desde.Value.ToUniversalTime());
        if (hasta.HasValue) query = query.Where(l => l.FechaHora <= hasta.Value.ToUniversalTime());

        var lecturas = await query
            .OrderByDescending(l => l.FechaHora)
            .Take(limite)
            .Select(l => new LecturaTemperaturaDto(
                l.idLecturaTemperatura,
                l.idSensor,
                l.sensor!.nombreSensor,
                l.temperatura,
                l.FechaHora))
            .ToListAsync();

        return Ok(lecturas);
    }

    /// <summary>Ultima lectura de cada sensor. Util para pintar el dashboard al cargar.</summary>
    [HttpGet("temperaturas/ultimas")]
    public async Task<IActionResult> UltimasTemperaturas()
    {
        // Subconsulta correlacionada por sensor: Postgres la resuelve con LATERAL,
        // y EF la traduce sin problema (a diferencia de GroupBy + First).
        var ultimas = await _db.Sensor
            .AsNoTracking()
            .Select(s => _db.LecturaTemperaturas
                .Where(l => l.idSensor == s.idSensor)
                .OrderByDescending(l => l.FechaHora)
                .Select(l => new LecturaTemperaturaDto(
                    l.idLecturaTemperatura,
                    l.idSensor,
                    s.nombreSensor,
                    l.temperatura,
                    l.FechaHora))
                .FirstOrDefault())
            .ToListAsync();

        return Ok(ultimas.Where(l => l is not null));
    }
}
