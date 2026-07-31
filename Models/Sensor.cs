using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RanchoMqttApi;

public class Sensor
{
    [Key]
    public int idSensor { get; set; }
    public string nombreSensor { get; set; } = ""; 
    public int idTipoSensor { get; set; }
    [ForeignKey("idTipoSensor")]
    public TipoSensor? tipoSensor { get; set; }
    public int idZona { get; set; } 
    [ForeignKey("idZona")]
    public Zona? zona { get; set; }

}
