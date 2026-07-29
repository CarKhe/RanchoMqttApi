
using MQTTnet;

namespace RanchoMqttApi;

public class MqttPublisherService : IMqttPublisherService
{
    private readonly MqttClientFactory _factory = new();
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;

    public MqttPublisherService()
    {
        _client = _factory.CreateMqttClient();
        _options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .Build();
    }
    public async Task PublishAsync(string topic, string payload)
    {
        await AsegurarConexionAsync();

        var mensaje = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build();

        await _client.PublishAsync(mensaje, CancellationToken.None);
        Console.WriteLine($"[API publicó] {topic} -> {payload}");
    }

    private async Task AsegurarConexionAsync()
    {
        if (!_client.IsConnected)
            await _client.ConnectAsync(_options, CancellationToken.None);
    }
}
