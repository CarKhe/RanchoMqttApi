namespace RanchoMqttApi;

public interface IReleService
{
    Task<(bool exito, string mensaje)> CambiarEstadoAsync(string tipo, int id, bool estado);
}
