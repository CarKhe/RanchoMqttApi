using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RanchoMqttApi;

namespace MyApp.Namespace
{
    public record LoginRequest(string UserMail, string Password);
    
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
    private readonly DBContext _db;
    private readonly IJwtService _jwtService;

    public AuthController(DBContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.userMail == request.UserMail);
        if (user is null)
            return Unauthorized("Credenciales inválidas");

        var hasher = new PasswordHasher<Users>();
        var result = hasher.VerifyHashedPassword(user, user.passwordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            return Unauthorized("Credenciales inválidas");

        user.updatedLogin = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        return Ok(new { token });
    }
    }
}
