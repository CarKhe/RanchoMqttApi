
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace RanchoMqttApi;

public class TemperaturaHandler : IMqttTopicHandler
{
    private readonly DBContext _db;
    private readonly ILogger<TemperaturaHandler> _logger;
    private readonly IHubContext<RelesHub> _hubContext;
    public TemperaturaHandler(DBContext db,ILogger<TemperaturaHandler> logger, IHubContext<RelesHub> hubContext)
    {
        _db = db;
        _logger = logger;
        _hubContext = hubContext;
    }
    
    public bool PuedeManejar(string topic) => MqttTopics.EsTopicDeLecturaSensor(topic);

    public async Task ManejarAsync(string topic, string payload)
    {
        var partes = topic.Split('/');
        var tipo = partes[2];
        var id = int.Parse(partes[3]);

        var sensor = await _db.Sensor
            .Include(s => s.tipoSensor)
            .FirstOrDefaultAsync(s => s.idSensor == id && s.tipoSensor!.nombreSensor == tipo);

        if (sensor is null)
        {
            _logger.LogWarning(LogMessages.SensorNoExiste, tipo, id);
            return;
        }

        var temperatura = double.Parse(payload);
        var fecha = DateTime.UtcNow;

        _db.LecturaTemperaturas.Add(new LecturaTemperatura
        {
            idSensor = sensor.idSensor,
            temperatura = temperatura,
            FechaHora = fecha
        });
        await _db.SaveChangesAsync();
        await _hubContext.Clients.All.SendAsync(HubMethods.TemperaturaActualizada, tipo, sensor.idSensor, temperatura, fecha);
    }


}
