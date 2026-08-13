namespace RanchoMqttApi;

public interface IProgramacionService
{
    Task<List<ProgramacionDto>> ObtenerTodasAsync();
    Task<List<CorridaDto>> ObtenerCorridasDeHoyAsync(CancellationToken ct);

    Task<(bool exito, string mensaje, int? id)> CrearAsync(CrearProgramacionDto dto);
    Task<(bool exito, string mensaje)> ActualizarAsync(int id, CrearProgramacionDto dto);
    Task<(bool exito, string mensaje)> EliminarAsync(int id);
    Task<(bool exito, string mensaje)> CambiarHabilitadaAsync(int id, bool valor);
    

    Task<(bool exito, string mensaje)> CancelarHoyAsync(int idProgramacion, CancellationToken ct);
}
