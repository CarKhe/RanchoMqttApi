using Microsoft.EntityFrameworkCore;

namespace RanchoMqttApi;

public class ReleService : IReleService
{
    private readonly IMqttPublisherService _mqttPublisher;
    private readonly DBContext _db;
    private readonly IReleCacheService _cache;

    private readonly IComandoTimeoutService _timeoutService;

    public ReleService(IMqttPublisherService mqttPublisher, DBContext db, 
        IReleCacheService cache, IComandoTimeoutService timeoutService)
    {
        _mqttPublisher = mqttPublisher;
        _db = db;
        _cache = cache;
        _timeoutService = timeoutService;
    }

    public async Task<(bool exito, string mensaje)> CambiarEstadoAsync(
        string tipo, int id, bool estado,OrigenComando origen = OrigenComando.Manual)
    {
        var rele = await _db.Rele
            .Include(r => r.tipoRele)
            .FirstOrDefaultAsync(r => r.idRele == id && r.tipoRele!.nombreRele == tipo);

        if (rele is null)
            return (false, $"El relevador '{tipo}/{id}' no existe en el catálogo");

        // Fase 5: aquí va la cancelación de la corrida de hoy
        // if (!estado && origen == OrigenComando.Manual)
        //     await CancelarDetalleEnCursoAsync(id);

        var topic = MqttTopics.ReleCmd(tipo, id);
        var payload = estado ? "on" : "off";

        try
        {
            await _mqttPublisher.PublishAsync(topic, payload);
            _timeoutService.IniciarMonitoreo(tipo, id);
            return (true, $"Comando enviado a {topic}: {payload}");
        }
        catch(Exception ex)
        {
            throw new MqttNoDisponibleException(
                "No se pudo conectar con el broker MQTT. Intenta de nuevo en unos segundos.", ex);
        }
    }

    public async Task<List<ReleEstadoDto>> ObtenerTodosConEstadoAsync()
    {
        var reles = await _db.Rele
            .Include(r => r.tipoRele)
            .Include(r => r.zona)
            .ToListAsync();

        return reles.Select(r =>
        {
            var enCache = _cache.Obtener(r.tipoRele!.nombreRele, r.idRele);
            return new ReleEstadoDto(
                r.idRele,
                r.Nombre,
                r.tipoRele!.nombreRele,
                r.zona!.zonaName,
                enCache?.Estado,
                enCache?.UltimaConfirmacion
            );
        }).ToList();
    }
}
