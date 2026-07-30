using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RanchoMqttApi;

public class HistorialEstadoRelei
{
    [Key]
    public int idHistorialEstadoRelei { get; set; }
    public int idRele { get; set; }
    [ForeignKey("idRele")]
    public required Rele rele { get; set; }

    public string estado { get; set; } = "";
    public bool exito { get; set; }
    public DateTime fechaHora { get; set; }
}
