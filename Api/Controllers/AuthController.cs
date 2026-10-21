using Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Controlador de autenticación.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // TODO: Inyectar el servicio de autenticación
    public AuthController() { }

    [HttpPost("login")]
    public IActionResult Login()
    {
        // TODO: Implementar lógica de login
        return Ok(ApiResponse<string>.Ok("Token aquí", "Login exitoso"));
    }
}
