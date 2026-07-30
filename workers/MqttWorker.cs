using Microsoft.AspNetCore.SignalR;
using MQTTnet;

namespace RanchoMqttApi.Workers;

public class MqttWorker : BackgroundService
{
    public record EstadoRele(string estado, bool exito);
    private readonly MqttClientFactory _mqttFactory = new();
    private readonly IHubContext<RelesHub> _hubContext;
    private readonly IReleCacheService _cache;

    public MqttWorker(IHubContext<RelesHub> hubContext, IReleCacheService cache) 
    {
        _hubContext = hubContext;
        _cache      =      cache;
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
                var id = int.Parse(partes[3]); // ahora como int, para la cache
                Console.WriteLine($"[API recibió estado] {tipo} {id} -> {payload}");
                var datos = System.Text.Json.JsonSerializer.Deserialize<EstadoRele>(payload);

                var anterior = _cache.Obtener(tipo, id); // NUEVO: qué tenia antes la cache
                var huboCambio = anterior is null || anterior.Estado != datos!.estado;
                 _cache.Actualizar(tipo, id, datos!.estado);
                if (huboCambio)
                {
                    if (anterior is not null)
                        Console.WriteLine($"[RECONCILIACION] {tipo} {id}: cache decia '{anterior.Estado}', ESP32 confirma '{datos.estado}' -> corregido");
                    else
                        Console.WriteLine($"[API] {tipo} {id} confirmado por primera vez: {datos.estado}");

                    await _hubContext.Clients.All.SendAsync("EstadoActualizado", tipo, id, datos.estado, datos.exito);
                }
                else
                {
                    Console.WriteLine($"[Heartbeat OK] {tipo} {id} sigue en '{datos.estado}', sin cambios"); // NUEVO
                }
            }
            else if (topic == "rancho/temp")
            {
                Console.WriteLine($"[API recibió temperatura] {payload}");
            }
            else if (topic == "rancho/esp32/conexion") // NUEVO
            {
                Console.WriteLine($"[API] ESP32 esta: {payload}");
                await _hubContext.Clients.All.SendAsync("ConexionActualizada", payload);
            }
        };

        await mqttClient.ConnectAsync(options, stoppingToken);
        Console.WriteLine("API conectada al broker MQTT");

        var subscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f => f.WithTopic("rancho/temp"))
            .WithTopicFilter(f => f.WithTopic("rancho/reles/+/+/estado"))
            .WithTopicFilter(f => f.WithTopic("rancho/esp32/conexion"))
            .Build();

        await mqttClient.SubscribeAsync(subscribeOptions, stoppingToken);
        Console.WriteLine("API suscrita a rancho/temp y rancho/reles/+/+/estado");
    }
}

