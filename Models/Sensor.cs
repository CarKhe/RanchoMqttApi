using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RanchoMqttApi;

public class Sensor
{
    [Key]
    public int idSensor { get; set; }
    public string nombreSensor { get; set; } = ""; 
    public int idZona { get; set; } 
    [ForeignKey("idZona")]
    public required Zona zona { get; set; }

}
