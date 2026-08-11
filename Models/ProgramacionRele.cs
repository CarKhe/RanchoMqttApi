using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RanchoMqttApi;

public class ProgramacionRele
{
    [Key]
    public int idProgramacionRele { get; set; }

    public int idProgramacion { get; set; }
    [ForeignKey("idProgramacion")]
    public ProgramacionRiego? programacion { get; set; }

    public int idRele { get; set; }
    [ForeignKey("idRele")]
    public Rele? rele { get; set; }

    public int duracionMinutos { get; set; }
    public int orden { get; set; }
}
