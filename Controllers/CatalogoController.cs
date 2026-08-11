using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RanchoMqttApi;

/// <summary>
/// Consultas de solo lectura sobre el catalogo: zonas, tipos, reles y sensores.
/// </summary>
[Route("api/[controller]")]
[Authorize]
[ApiController]
public class CatalogoController : ControllerBase
{
    private readonly DBContext _db;

    public CatalogoController(DBContext db) => _db = db;

    [HttpGet("zonas")]
    public async Task<IActionResult> Zonas()
    {
        var zonas = await _db.Zona
            .AsNoTracking()
            .OrderBy(z => z.idZona)
            .Select(z => new ZonaDto(z.idZona, z.zonaName))
            .ToListAsync();

        return Ok(zonas);
    }

    [HttpGet("tipos-rele")]
    public async Task<IActionResult> TiposRele()
    {
        var tipos = await _db.TipoReles
            .AsNoTracking()
            .OrderBy(t => t.idTipoRele)
            .Select(t => new TipoDto(t.idTipoRele, t.nombreRele))
            .ToListAsync();

        return Ok(tipos);
    }

    [HttpGet("tipos-sensor")]
    public async Task<IActionResult> TiposSensor()
    {
        var tipos = await _db.TipoSensores
            .AsNoTracking()
            .OrderBy(t => t.idTipoSensor)
            .Select(t => new TipoDto(t.idTipoSensor, t.nombreSensor))
            .ToListAsync();

        return Ok(tipos);
    }

    /// <summary>Reles del catalogo. Para el estado en vivo usa GET /api/reles/estados.</summary>
    [HttpGet("reles")]
    public async Task<IActionResult> Reles([FromQuery] int? idZona)
    {
        var query = _db.Rele.AsNoTracking().AsQueryable();

        if (idZona.HasValue)
            query = query.Where(r => r.idZona == idZona.Value);

        var reles = await query
            .OrderBy(r => r.idRele)
            .Select(r => new ReleDto(
                r.idRele,
                r.Nombre,
                r.tipoRele!.nombreRele,
                r.zona!.zonaName))
            .ToListAsync();

        return Ok(reles);
    }

    [HttpGet("sensores")]
    public async Task<IActionResult> Sensores([FromQuery] int? idZona)
    {
        var query = _db.Sensor.AsNoTracking().AsQueryable();

        if (idZona.HasValue)
            query = query.Where(s => s.idZona == idZona.Value);

        var sensores = await query
            .OrderBy(s => s.idSensor)
            .Select(s => new SensorDto(
                s.idSensor,
                s.nombreSensor,
                s.tipoSensor!.nombreSensor,
                s.zona!.zonaName))
            .ToListAsync();

        return Ok(sensores);
    }
}
