using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        
//TEMPORALES
        [HttpPost("tick-manual")]
        public async Task<IActionResult> TickManual(
            [FromServices] IMotorProgramacionService motor, CancellationToken ct)
        {
            await motor.TickAsync(ct);
            return Ok(new { mensaje = "Tick ejecutado" });
        }

        [HttpGet("corridas")]
        public async Task<IActionResult> Corridas([FromServices] DBContext db, CancellationToken ct)
        {
            var corridas = await db.EjecucionesProgramacion
                .OrderByDescending(e => e.fecha)
                .ThenByDescending(e => e.idEjecucion)
                .Take(5)
                .AsNoTracking()
                .Select(e => new
                {
                    e.idEjecucion,
                    e.idProgramacion,
                    e.fecha,
                    estado = e.estado.ToString(),
                    e.inicioReal,
                    e.finReal,
                    detalles = e.detalles.OrderBy(d => d.orden).Select(d => new
                    {
                        d.idRele,
                        d.orden,
                        d.duracionMinutos,
                        estado = d.estado.ToString(),
                        d.inicioReal,
                        d.finPrevisto,
                        d.finReal
                    })
                })
                .ToListAsync(ct);

            return Ok(corridas);
        }
    }
}
