namespace RanchoMqttApi;

public interface IMqttPublisherService
{
    Task PublishAsync(string topic, string payload);

}
