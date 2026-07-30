using System.ComponentModel.DataAnnotations;

namespace RanchoMqttApi;

public class Zona
{
    [Key]
    public int idZona { get; set; }
    public string zonaName { get; set; } = "";
}
