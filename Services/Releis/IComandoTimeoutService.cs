namespace RanchoMqttApi;

public interface IComandoTimeoutService
{
    void IniciarMonitoreo(string tipo, int id);
}
