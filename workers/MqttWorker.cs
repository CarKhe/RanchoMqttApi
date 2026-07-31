using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using MQTTnet;

namespace RanchoMqttApi.Workers;

public class MqttWorker : BackgroundService
{
    private readonly MqttClientFactory _mqttFactory = new();
    private readonly IServiceScopeFactory _scopeFactory;

    public MqttWorker(IServiceScopeFactory scopeFactory) 
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttClient = _mqttFactory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithTcpServer("localhost", 1883)
            .Build();

        mqttClient.ApplicationMessageReceivedAsync += async e => // ahora async
        {
            var topic = e.ApplicationMessage.Topic;
            var payload = e.ApplicationMessage.ConvertPayloadToString();

            using var scope = _scopeFactory.CreateScope(); // NUEVO: un scope por mensaje

            var handlers = scope.ServiceProvider.GetServices<IMqttTopicHandler>();
            var handler = handlers.FirstOrDefault(h => h.PuedeManejar(topic));

            if (handler is not null)
                await handler.ManejarAsync(topic, payload);
            else
                Console.WriteLine($"[Sin manejador] Topic no reconocido: {topic}");

        };

        await mqttClient.ConnectAsync(options, stoppingToken);
        Console.WriteLine("API conectada al broker MQTT");

        var subscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic(MqttTopics.Temperatura))
            .WithTopicFilter(f => f.WithTopic(MqttTopics.ReleEstadoWildcard))
            .WithTopicFilter(f => f.WithTopic(MqttTopics.Conexion))
            .Build();

        await mqttClient.SubscribeAsync(subscribeOptions, stoppingToken);
        Console.WriteLine("API suscrita a rancho/temp y rancho/reles/+/+/estado");
    }
}

