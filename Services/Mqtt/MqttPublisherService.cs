
using MQTTnet;

namespace RanchoMqttApi;

public class MqttPublisherService : IMqttPublisherService
{
    private readonly MqttClientFactory _factory = new();
    private readonly IMqttClient _client;
    private readonly MqttClientOptions _options;
    private readonly ILogger<MqttPublisherService> _logger;
    private readonly IConfiguration _config;

    public MqttPublisherService(ILogger<MqttPublisherService> logger,
        IConfiguration config)
    {
        _client = _factory.CreateMqttClient();
        _options = MqttOpciones.Construir(config).Build();
        _logger = logger;
        _config = config;
    }
    public async Task PublishAsync(string topic, string payload)
    {
        await AsegurarConexionAsync();

        var mensaje = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build();

        await _client.PublishAsync(mensaje, CancellationToken.None);
        _logger.LogInformation(LogMessages.APIPublicacion,topic,payload);
    }

    private async Task AsegurarConexionAsync()
    {
        if (!_client.IsConnected)
            await _client.ConnectAsync(_options, CancellationToken.None);
    }
}
