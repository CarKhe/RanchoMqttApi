using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RanchoMqttApi;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class RelesController : ControllerBase
    {
        private readonly IMqttPublisherService _mqttPublisher;
        private readonly IReleService _releService;
        private readonly IReleCacheService _cache;
        private static readonly string[] TiposValidos = { "riego", "focos" };

        public RelesController(IMqttPublisherService mqttPublisher, IReleService releService, IReleCacheService cache)
        {
            _mqttPublisher = mqttPublisher;
            _releService   =   releService;
            _cache         =         cache;
        }

        [HttpGet("estados")]
        public IActionResult ObtenerEstados()
        {
            var estados = _cache.ObtenerTodos().Values;
            return Ok(estados);
        }

        [HttpPatch("{tipo}/{id}/cambiar")]
        public async Task<IActionResult> Cambiar(string tipo, int id, [FromQuery] bool estado)
        {
            var (exito, mensaje) = await _releService.CambiarEstadoAsync(tipo, id, estado);
            if (!exito)
            {
                return BadRequest(new { mensaje });
            }
            return Accepted(new { mensaje = "Comando enviado, esperando confirmación del dispositivo" });
        }
    }
}
