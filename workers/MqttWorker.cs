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
    private readonly IConfiguration _config;

    public MqttWorker(IServiceScopeFactory scopeFactory, 
        IHubContext<RelesHub> hubContext,ILogger<MqttWorker> logger, IConfiguration config) 
    {
        _scopeFactory = scopeFactory;
        _hubContext   =   hubContext;
        _logger       =       logger;
        _config       =       config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttClient = _mqttFactory.CreateMqttClient();

        var options = MqttOpciones.Construir(_config).Build();

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
            {
                _logger.LogWarning(LogMessages.TopicNoReconocido, topic);
            }
                
        };

        // NUEVO: si la conexión se cae despues de haber funcionado, reintenta sola
        mqttClient.DisconnectedAsync += async e =>
        {
            _logger.LogWarning(LogMessages.ConexionPerdida);
            await _hubContext.Clients.All.SendAsync(HubMethods.BrokerDesconectado, LogMessages.ConexionPerdida);
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
                    _logger.LogInformation(LogMessages.ApiConectada);

                    var subscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
                        .WithTopicFilter(f => f.WithTopic(MqttTopics.SensorLecturaWildcard))
                        .WithTopicFilter(f => f.WithTopic(MqttTopics.ReleEstadoWildcard))
                        .WithTopicFilter(f => f.WithTopic(MqttTopics.Conexion))
                        .Build();

                    await mqttClient.SubscribeAsync(subscribeOptions, stoppingToken);
                    _logger.LogInformation(LogMessages.ApiSuscrita);
                    await _hubContext.Clients.All.SendAsync(HubMethods.BrokerConectado,LogMessages.ConexionRestaurada);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,LogMessages.ErrorConexionReintento);
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

