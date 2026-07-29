namespace RanchoMqttApi;

public class ReleService : IReleService
{
    private readonly IMqttPublisherService _mqttPublisher;
    private static readonly string[] TiposValidos = { "riego", "focos" };

    public ReleService(IMqttPublisherService mqttPublisher)
    {
        _mqttPublisher = mqttPublisher;
    }

    public async Task<(bool exito, string mensaje)> CambiarEstadoAsync(string tipo, int id, bool estado)
    {
        if (!TiposValidos.Contains(tipo))
            return (false, $"Tipo '{tipo}' no reconocido. Válidos: {string.Join(", ", TiposValidos)}");

        var topic = $"rancho/reles/{tipo}/{id}/cmd";
        var payload = estado ? "on" : "off";

        await _mqttPublisher.PublishAsync(topic, payload);
        return (true, $"Comando enviado a {topic}: {payload}");
    }
}
