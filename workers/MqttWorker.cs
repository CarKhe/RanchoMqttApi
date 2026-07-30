using Microsoft.AspNetCore.SignalR;
using MQTTnet;

namespace RanchoMqttApi.Workers;

public class MqttWorker : BackgroundService
{
    public record EstadoRele(string estado, bool exito);
    private readonly MqttClientFactory _mqttFactory = new();
    private readonly IHubContext<RelesHub> _hubContext;

    public MqttWorker(IHubContext<RelesHub> hubContext) 
    {
        _hubContext = hubContext;
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

            if (topic.StartsWith("rancho/reles/"))
            {
                var partes = topic.Split('/'); // rancho, reles, riego, 12, estado
                var tipo = partes[2];
                var id = partes[3];
                Console.WriteLine($"[API recibió estado] {tipo} {id} -> {payload}");
                var datos = System.Text.Json.JsonSerializer.Deserialize<EstadoRele>(payload);
                if (datos!.exito)
                        Console.WriteLine($"[API] {tipo} {id} confirmado: {datos.estado}");
                    else
                        Console.WriteLine($"[API] {tipo} {id} FALLO, no cambió");

                    await _hubContext.Clients.All.SendAsync("EstadoActualizado", tipo, id, datos.estado, datos.exito);
            }
            else if (topic == "rancho/temp")
            {
                Console.WriteLine($"[API recibió temperatura] {payload}");
            }
        };

        await mqttClient.ConnectAsync(options, stoppingToken);
        Console.WriteLine("API conectada al broker MQTT");

        var subscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic("rancho/temp"))
            .WithTopicFilter(f => f.WithTopic("rancho/reles/+/+/estado"))
            .Build();

        await mqttClient.SubscribeAsync(subscribeOptions, stoppingToken);
        Console.WriteLine("API suscrita a rancho/temp y rancho/reles/+/+/estado");
    }
}

