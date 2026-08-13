using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace RanchoMqttApi
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class RelesController : ControllerBase
    {
        private readonly IReleService _releService;
        private readonly RiegoOptions _riego;
        private readonly IProgramacionService _programacionService;

        public RelesController(IReleService releService, IOptions<RiegoOptions> riego,
            IProgramacionService programacionService)
        {
            _releService = releService;
            _riego = riego.Value;   
            _programacionService = programacionService;
        }

        [HttpGet("estados")]
        public async Task<IActionResult> ObtenerEstados()
        {
            var estados = await _releService.ObtenerTodosConEstadoAsync();
            return Ok(estados);
        }

        [HttpPatch("{tipo}/{id}/cambiar")]
        public async Task<IActionResult> Cambiar(string tipo, int id, [FromQuery] bool estado)
        {
            try
            {
                var (exito, mensaje) = await _releService.CambiarEstadoAsync(tipo, id, estado);
                if (!exito)
                {
                    return BadRequest(new { mensaje });
                }
                return Accepted(new { mensaje = ControllersConstants.CoamndoEnviado });
            }
            catch (MqttNoDisponibleException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensaje = ex.Message });
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> Actualizar(int id, [FromBody] CrearProgramacionDto dto)
        {
            var (exito, mensaje) = await _programacionService.ActualizarAsync(id, dto);
            return exito ? Ok(new { mensaje }) : BadRequest(new { mensaje });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var (exito, mensaje) = await _programacionService.EliminarAsync(id);
            return exito ? Ok(new { mensaje }) : BadRequest(new { mensaje });
        }

        [HttpPatch("{id}/habilitada")]
        public async Task<IActionResult> CambiarHabilitada(int id, [FromQuery] bool valor)
        {
            var (exito, mensaje) = await _programacionService.CambiarHabilitadaAsync(id, valor);
            return exito ? Ok(new { mensaje }) : BadRequest(new { mensaje });
        }

        [HttpPost("{id}/cancelar-hoy")]
        public async Task<IActionResult> CancelarHoy(int id, CancellationToken ct)
        {
            var (exito, mensaje) = await _programacionService.CancelarHoyAsync(id, ct);
            return exito ? Ok(new { mensaje }) : BadRequest(new { mensaje });
        }
    }
}
