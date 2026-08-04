namespace RanchoMqttApi;

public interface IJwtService
{
    string GenerateToken(Users user);
}
