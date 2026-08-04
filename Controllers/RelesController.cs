using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RanchoMqttApi
{
    [Route("api/[controller]")]
    [Authorize]
    [ApiController]
    public class RelesController : ControllerBase
    {
        private readonly IReleService _releService;

        public RelesController(IReleService releService)
        {
            _releService = releService;
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
    }
}
