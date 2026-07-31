namespace RanchoMqttApi;

public class MqttNoDisponibleException : Exception
{
    public MqttNoDisponibleException(string mensaje, Exception inner) : base(mensaje, inner) { }
}
