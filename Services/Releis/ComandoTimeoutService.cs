using Microsoft.AspNetCore.SignalR;

namespace RanchoMqttApi;

public class ComandoTimeoutService : IComandoTimeoutService
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly IReleCacheService _cache;
    private readonly IHubContext<RelesHub> _hubContext;

    public ComandoTimeoutService(IReleCacheService cache, IHubContext<RelesHub> hubContext)
    {
        _cache = cache;
        _hubContext = hubContext;
    }
    public void IniciarMonitoreo(string tipo, int id)
    {
        var momentoEnvio = DateTime.UtcNow;

        _ = Task.Run(async () =>
        {
            await Task.Delay(Timeout);

            var actual = _cache.Obtener(tipo, id);
            var llegoConfirmacion = actual is not null && actual.UltimaConfirmacion > momentoEnvio;

            if (!llegoConfirmacion)
            {
                await _hubContext.Clients.All.SendAsync(
                    HubMethods.ComandoExpirado, tipo, id,
                    $"El dispositivo no respondió el comando para {tipo}/{id} en {Timeout.TotalSeconds} segundos");
            }
        });
    }
}
