using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace RanchoMqttApi;

public class AuthService : IAuthService
{
    private readonly DBContext _db;
    private readonly IJwtService _jwtService;

    public AuthService(DBContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<(bool exito, string mensaje, string? token)> LoginAsync(string userMail, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.userMail == userMail);
        if (user is null)
            return (false, AuthConstants.CredencialesInvalidas, null);

        var hasher = new PasswordHasher<Users>();
        var result = hasher.VerifyHashedPassword(user, user.passwordHash, password);

        if (result == PasswordVerificationResult.Failed)
            return (false, AuthConstants.CredencialesInvalidas, null);

        user.updatedLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        return (true, AuthConstants.LoginExitoso, token);
    }

    public async Task<(bool exito, string mensaje)> ChangePasswordAsync(int idUser, string currentPassword, string newPassword)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.idUser == idUser);
        if (user is null)
            return (false, AuthConstants.CredencialesInvalidas);

        var hasher = new PasswordHasher<Users>();
        var result = hasher.VerifyHashedPassword(user, user.passwordHash, currentPassword);
        if (result == PasswordVerificationResult.Failed)
            return (false, AuthConstants.CredencialesInvalidas);

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < AuthConstants.PasswordLongitudMinima)
            return (false, AuthConstants.PasswordMuyCorta);

        user.passwordHash = GenerarHashPassword(user, newPassword);
        await _db.SaveChangesAsync();

        return (true, AuthConstants.PasswordActualizada);
    }

    private static string GenerarHashPassword(Users user, string password)
    {
        var hasher = new PasswordHasher<Users>();
        return hasher.HashPassword(user, password);
    }
}
