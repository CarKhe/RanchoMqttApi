

using Microsoft.AspNetCore.SignalR;

namespace RanchoMqttApi;

public class ConexionHandler : IMqttTopicHandler
{
    private readonly IHubContext<RelesHub> _hubContext;

    public ConexionHandler(IHubContext<RelesHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public bool PuedeManejar(string topic) => topic == MqttTopics.Conexion;

    public async Task ManejarAsync(string topic, string payload)
    {
        await _hubContext.Clients.All.SendAsync("ConexionActualizada", payload);
    }
}
