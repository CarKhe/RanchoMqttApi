using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RanchoMqttApi;

public class LecturaTemperatura
{
    [Key]
    public int idLecturaTemperatura { get; set; }
    public int idSensor { get; set; }
    [ForeignKey("idSensor")]
    public Sensor? sensor { get; set; }
    public double temperatura { get; set; }
    public DateTime FechaHora { get; set; }
}
