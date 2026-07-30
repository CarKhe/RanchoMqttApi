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
        private static readonly string[] TiposValidos = { "riego", "focos" };

        public RelesController(IMqttPublisherService mqttPublisher, IReleService releService)
        {
            _mqttPublisher = mqttPublisher;
            _releService   =   releService;
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
