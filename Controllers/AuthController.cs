using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RanchoMqttApi;

namespace MyApp.Namespace
{
    public record LoginRequest(string UserMail, string Password);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var (exito, mensaje, token) = await _authService.LoginAsync(request.UserMail, request.Password);
        return exito ? Ok(new { token }) : Unauthorized(mensaje);
    }

    // Endpoint privado (requiere JWT) y oculto de Swagger: solo permite al usuario autenticado
    // cambiar su propia contraseña, no está pensado para exponerse en la documentación pública.
    [HttpPost("change-password")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var idUserClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idUserClaim, out var idUser))
            return Unauthorized();

        var (exito, mensaje) = await _authService.ChangePasswordAsync(
            idUser, request.CurrentPassword, request.NewPassword);

        return exito ? Ok(new { mensaje }) : BadRequest(new { mensaje });
    }
    }
}
