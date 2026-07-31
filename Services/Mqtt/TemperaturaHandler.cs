
namespace RanchoMqttApi;

public class TemperaturaHandler : IMqttTopicHandler
{
    private readonly DBContext _db;
    public TemperaturaHandler(DBContext db)
    {
        _db = db;
    }
    
    public bool PuedeManejar(string topic) => topic == MqttTopics.Temperatura;

    public async Task ManejarAsync(string topic, string payload)
    {
        _db.LecturaTemperaturas.Add(new LecturaTemperatura
        {
            idSensor = 1, // TODO: ajustar cuando el topic incluya el id real del sensor
            temperatura = double.Parse(payload),
            FechaHora = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }


}
