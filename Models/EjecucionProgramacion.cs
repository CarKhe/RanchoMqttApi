using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RanchoMqttApi;

public class EjecucionProgramacion
{
    [Key]
    public int idEjecucion { get; set; }

    public int idProgramacion { get; set; }
    [ForeignKey("idProgramacion")]
    public ProgramacionRiego? programacion { get; set; }

    public DateOnly fecha { get; set; }
    public EstadosEjecucion estado { get; set; } = EstadosEjecucion.Pendiente;

    public DateTime? inicioReal { get; set; }
    public DateTime? finReal { get; set; }

    public ICollection<EjecucionReleDetalle> detalles { get; set; } = [];
}
