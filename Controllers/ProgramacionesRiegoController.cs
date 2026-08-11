using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RanchoMqttApi;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProgramacionesRiegoController : ControllerBase
    {
    private readonly IProgramacionService _service;

    public ProgramacionesRiegoController(IProgramacionService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Obtener() => Ok(await _service.ObtenerTodasAsync());

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearProgramacionDto dto)
    {
        var (exito, mensaje, id) = await _service.CrearAsync(dto);
        if (!exito) return BadRequest(new { mensaje });
        return Ok(new { id, mensaje });
    }
    }
}
