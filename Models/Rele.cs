using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RanchoMqttApi;

public class Rele
{
    [Key]
    public int idRele { get; set; }
    public string Nombre { get; set; } = "";
    public int idZona { get; set; }
    [ForeignKey ("idZona")]
    public required Zona zona { get; set; }
}
