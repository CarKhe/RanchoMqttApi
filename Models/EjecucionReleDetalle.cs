using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RanchoMqttApi;

public class EjecucionReleDetalle
{
    [Key]
    public int idEjecucionDetalle { get; set; }

    public int idEjecucion { get; set; }
    [ForeignKey("idEjecucion")]
    public EjecucionProgramacion? ejecucion { get; set; }

    public int idRele { get; set; }
    [ForeignKey("idRele")]
    public Rele? rele { get; set; }

    public int orden { get; set; }
    public int duracionMinutos { get; set; }

    public EstadoDetalle estado { get; set; } = EstadoDetalle.Pendiente;

    public DateTime? inicioReal { get; set; }
    public DateTime? finPrevisto { get; set; }
    public DateTime? finReal { get; set; }
}
