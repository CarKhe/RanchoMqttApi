using System.ComponentModel.DataAnnotations;

namespace RanchoMqttApi;

public class ProgramacionRiego
{
    [Key]
    public int idProgramacion { get; set; }
    public string Nombre { get; set; } = "";
    public bool habilitada { get; set; } = true;

    public TimeOnly horaInicio { get; set; }
    public TimeOnly horaFin { get; set; }
    public int diasSemana { get; set; }

    public ModoEjecucion modoEjecucion { get; set; } = ModoEjecucion.Secuencial;
    public DateTime fechaCreacion { get; set; }

    public ICollection<ProgramacionRele> reles { get; set; } = [];
}
