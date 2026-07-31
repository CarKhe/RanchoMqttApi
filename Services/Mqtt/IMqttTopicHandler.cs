namespace RanchoMqttApi;

public interface IMqttTopicHandler
{
    bool PuedeManejar(string topic);
    Task ManejarAsync(string topic, string payload);
}
