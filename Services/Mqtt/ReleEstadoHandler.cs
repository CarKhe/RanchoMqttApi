
using Microsoft.AspNetCore.SignalR;

namespace RanchoMqttApi;

public class ReleEstadoHandler : IMqttTopicHandler
{
    public record EstadoRele(string estado, bool exito);
    
    private readonly IHubContext<RelesHub> _hubContext;
    private readonly IReleCacheService _cache;
    private readonly DBContext _db;
    public ReleEstadoHandler(IHubContext<RelesHub> hubContext, IReleCacheService cache, DBContext db)
    {
        _hubContext = hubContext;
        _cache = cache;
        _db = db;
    }

    public bool PuedeManejar(string topic) => MqttTopics.EsTopicDeEstadoRele(topic);


    public async Task ManejarAsync(string topic, string payload)
    {
        var partes = topic.Split('/');
        var tipo = partes[2];
        var id = int.Parse(partes[3]);
        var datos = System.Text.Json.JsonSerializer.Deserialize<EstadoRele>(payload)!;

        var anterior = _cache.Obtener(tipo, id);
        var huboCambio = anterior is null || anterior.Estado != datos.estado;
        _cache.Actualizar(tipo, id, datos.estado);

        _db.HistorialEstadoReleis.Add(new HistorialEstadoRelei
        {
            idRele = id,
            estado = datos.estado,
            exito = datos.exito,
            fechaHora = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        if (huboCambio)
            await _hubContext.Clients.All.SendAsync("EstadoActualizado", tipo, id, datos.estado, datos.exito);
    }
}
