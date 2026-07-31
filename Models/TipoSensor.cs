using System.ComponentModel.DataAnnotations;

namespace RanchoMqttApi;

public class TipoSensor
{
    [Key]
    public int idTipoSensor { get; set; }
    public string nombreSensor { get; set; } = "";
}
