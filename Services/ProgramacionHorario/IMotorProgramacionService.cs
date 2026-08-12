namespace RanchoMqttApi;

public interface IMotorProgramacionService
{
    Task TickAsync(CancellationToken ct);
}
