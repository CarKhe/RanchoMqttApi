namespace RanchoMqttApi;

public interface IAuthService
{
    Task<(bool exito, string mensaje, string? token)> LoginAsync(string userMail, string password);
    Task<(bool exito, string mensaje)> ChangePasswordAsync(int idUser, string currentPassword, string newPassword);
}
