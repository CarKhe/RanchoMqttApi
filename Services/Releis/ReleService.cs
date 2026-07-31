using Microsoft.EntityFrameworkCore;

namespace RanchoMqttApi;

public class ReleService : IReleService
{
    private readonly IMqttPublisherService _mqttPublisher;
    private readonly DBContext _db;

    public ReleService(IMqttPublisherService mqttPublisher, DBContext db)
    {
        _mqttPublisher = mqttPublisher;
        _db = db;
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
}
