using Microsoft.AspNetCore.SignalR;
using MQTTnet;

namespace RanchoMqttApi.Workers;

public class MqttWorker : BackgroundService
{
    private readonly SemaphoreSlim _reconectando = new(1, 1);
    private readonly MqttClientFactory _mqttFactory = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<RelesHub> _hubContext;
    private readonly ILogger<MqttWorker> _logger;

    public MqttWorker(IServiceScopeFactory scopeFactory, 
        IHubContext<RelesHub> hubContext,ILogger<MqttWorker> logger) 
    {
        _scopeFactory = scopeFactory;
        _hubContext   =   hubContext;
        _logger       =       logger;
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

        // NUEVO: si la conexión se cae despues de haber funcionado, reintenta sola
        mqttClient.DisconnectedAsync += async e =>
        {
            Console.WriteLine("Conexión MQTT perdida. Reintentando...");
            await _hubContext.Clients.All.SendAsync("BrokerDesconectado", "Se perdió la conexión con el broker MQTT");
            await ConectarYSuscribirAsync(mqttClient, options, stoppingToken);
        };

        await ConectarYSuscribirAsync(mqttClient, options, stoppingToken);
    }

    // NUEVO: bucle de reintento, usado tanto al arrancar como al reconectar
    private async Task ConectarYSuscribirAsync(IMqttClient mqttClient, MqttClientOptions options, CancellationToken stoppingToken)
    {
        if (!await _reconectando.WaitAsync(0, stoppingToken))
            return;
        try
        {
            while (!stoppingToken.IsCancellationRequested && !mqttClient.IsConnected)
            {
                try
                {
                    await mqttClient.ConnectAsync(options, stoppingToken);
                    _logger.LogInformation("API conectada al broker MQTT");

                    var subscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(f => f.WithTopic(MqttTopics.SensorLecturaWildcard))
                        .WithTopicFilter(f => f.WithTopic(MqttTopics.ReleEstadoWildcard))
                        .WithTopicFilter(f => f.WithTopic(MqttTopics.Conexion))
                        .Build();

                    await mqttClient.SubscribeAsync(subscribeOptions, stoppingToken);
                    Console.WriteLine("API suscrita a todos los topics");
                    await _hubContext.Clients.All.SendAsync("BrokerConectado","Se restauró la Conexión");
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo conectar/suscribir. Reintentando en 5 segundos...");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        finally
        {
            _reconectando.Release(); // libera el candado para el siguiente intento futuro
        }
    }
}

