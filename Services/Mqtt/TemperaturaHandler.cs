
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace RanchoMqttApi;

public class TemperaturaHandler : IMqttTopicHandler
{
    private readonly DBContext _db;
    private readonly ILogger<TemperaturaHandler> _logger;
    public TemperaturaHandler(DBContext db,ILogger<TemperaturaHandler> logger)
    {
        _db = db;
        _logger = logger;
        
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
            _logger.LogWarning($"[TemperaturaHandler] Sensor '{tipo}/{id}' no existe en el catálogo, se ignora la lectura.");
            return;
        }

        _db.LecturaTemperaturas.Add(new LecturaTemperatura
        {
            idSensor = sensor.idSensor,
            temperatura = double.Parse(payload),
            FechaHora = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }


}
