using Microsoft.EntityFrameworkCore;

namespace RanchoMqttApi;

public class ReleService : IReleService
{
    private readonly IMqttPublisherService _mqttPublisher;
    private readonly DBContext _db;
    private readonly IReleCacheService _cache;

    public ReleService(IMqttPublisherService mqttPublisher, DBContext db, IReleCacheService cache)
    {
        _mqttPublisher = mqttPublisher;
        _db = db;
        _cache = cache;
    }

    public async Task<(bool exito, string mensaje)> CambiarEstadoAsync(string tipo, int id, bool estado)
    {
        var rele = await _db.Rele
            .Include(r => r.tipoRele)
            .FirstOrDefaultAsync(r => r.idRele == id && r.tipoRele!.nombreRele == tipo);

        if (rele is null)
            return (false, $"El relevador '{tipo}/{id}' no existe en el catálogo");

        var topic = MqttTopics.ReleCmd(tipo, id);
        var payload = estado ? "on" : "off";

        await _mqttPublisher.PublishAsync(topic, payload);
        return (true, $"Comando enviado a {topic}: {payload}");
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
