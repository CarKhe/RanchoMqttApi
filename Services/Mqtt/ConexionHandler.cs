

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace RanchoMqttApi;

public class ConexionHandler : IMqttTopicHandler
{
    private readonly IHubContext<RelesHub> _hubContext;
    private readonly DBContext _db;
    private readonly ILogger<ConexionHandler> _logger;

    public ConexionHandler(IHubContext<RelesHub> hubContext,
        DBContext db,
        ILogger<ConexionHandler> logger)
    {
        _hubContext = hubContext;
         _db = db;
        _logger = logger;
    }

    public bool PuedeManejar(string topic) => topic == MqttTopics.Conexion;

    public async Task ManejarAsync(string topic, string payload)
    {
        await _hubContext.Clients.All.SendAsync(HubMethods.ConexionActualizada, payload);

        if (!payload.Equals("offline", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("ESP32 conectado");
            return;
        }

        _logger.LogWarning("ESP32 desconectado; revisando riegos en curso");
        await AbortarRiegosEnCursoAsync();
    }

    private async Task AbortarRiegosEnCursoAsync()
    {
        var abiertas = await _db.EjecucionesProgramacion
            .Include(e => e.programacion)
            .Include(e => e.detalles)
            .Where(e => e.estado == EstadosEjecucion.EnCurso)
            .ToListAsync();

        if (abiertas.Count == 0)
        {
            _logger.LogInformation("No había riegos en curso al desconectarse el ESP32");
            return;
        }

        var ahora = DateTime.UtcNow;

        foreach (var corrida in abiertas)
        {
            var abiertos = corrida.detalles
                .Where(d => d.estado == EstadoDetalle.EnCurso)
                .ToList();

            foreach (var d in abiertos)
            {
                d.estado = EstadoDetalle.Fallida;
                d.finReal = ahora;
            }

            foreach (var d in corrida.detalles.Where(d => d.estado == EstadoDetalle.Pendiente))
                d.estado = EstadoDetalle.Omitida;

            corrida.estado = EstadosEjecucion.Fallida;
            corrida.finReal = ahora;

            _logger.LogWarning(
                "Riego '{Nombre}' abortado: el ESP32 se desconectó con {N} relé(s) abierto(s)",
                corrida.programacion!.Nombre, abiertos.Count);

            await _hubContext.Clients.All.SendAsync(
                HubMethods.EjecucionActualizada, corrida.idProgramacion);
        }

        await _db.SaveChangesAsync();
    }
}
