using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RanchoMqttApi;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class ProgramacionesRiegoController : ControllerBase
{
    private readonly IProgramacionService _service;

    public ProgramacionesRiegoController(IProgramacionService service) => _service = service;

    // ---- reglas ----

    [HttpGet]
    public async Task<IActionResult> Obtener() => Ok(await _service.ObtenerTodasAsync());

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearProgramacionDto dto)
    {
        var (exito, mensaje, id) = await _service.CrearAsync(dto);
        return exito ? Ok(new { id, mensaje }) : BadRequest(new { mensaje });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] CrearProgramacionDto dto)
    {
        var (exito, mensaje) = await _service.ActualizarAsync(id, dto);
        return exito ? Ok(new { mensaje }) : BadRequest(new { mensaje });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var (exito, mensaje) = await _service.EliminarAsync(id);
        return exito ? Ok(new { mensaje }) : BadRequest(new { mensaje });
    }

    [HttpPatch("{id}/habilitada")]
    public async Task<IActionResult> CambiarHabilitada(int id, [FromQuery] bool valor)
    {
        var (exito, mensaje) = await _service.CambiarHabilitadaAsync(id, valor);
        return exito ? Ok(new { mensaje }) : BadRequest(new { mensaje });
    }

    // ---- corridas ----

    [HttpGet("corridas/hoy")]
    public async Task<IActionResult> CorridasDeHoy(CancellationToken ct)
        => Ok(await _service.ObtenerCorridasDeHoyAsync(ct));

    [HttpPost("{id}/cancelar-hoy")]
    public async Task<IActionResult> CancelarHoy(int id, CancellationToken ct)
    {
        var (exito, mensaje) = await _service.CancelarHoyAsync(id, ct);
        return exito ? Ok(new { mensaje }) : BadRequest(new { mensaje });
    }

    // ---- temporal, se va en la Fase 7 ----

    [HttpPost("tick-manual")]
    public async Task<IActionResult> TickManual(
        [FromServices] IMotorProgramacionService motor,
        [FromServices] IWebHostEnvironment env,
        CancellationToken ct)
    {
        if (!env.IsDevelopment()) return NotFound();

        await motor.TickAsync(ct);
        return Ok(new { mensaje = "Tick ejecutado" });
    }
}