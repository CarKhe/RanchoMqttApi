using System.ComponentModel.DataAnnotations;

namespace RanchoMqttApi;

public class TipoRele
{
    [Key]
    public int idTipoRele { get; set; }
    public string nombreRele { get; set; } = "";
}
