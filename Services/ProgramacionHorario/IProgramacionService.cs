namespace RanchoMqttApi;

public interface IProgramacionService
{
    Task<List<ProgramacionDto>> ObtenerTodasAsync();
    Task<(bool exito, string mensaje, int? id)> CrearAsync(CrearProgramacionDto dto);
}
