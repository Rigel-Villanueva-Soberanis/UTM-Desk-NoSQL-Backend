using Api.Common;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class UsuarioController : ControllerBase
{
    private readonly UsuarioService _service;

    public UsuarioController(UsuarioService service)
    {
        _service = service;
    }

    /// <summary>
    /// Obtiene todos los usuarios del sistema. (Solo soporte)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "soporte")]
    public async Task<IActionResult> GetAll()
    {
        var resultado = await _service.GetAllAsync();
        return Ok(ApiResponse<object>.Ok(resultado));
    }

    /// <summary>
    /// Obtiene un usuario por su ID de MongoDB. (Solo soporte)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "soporte")]
    public async Task<IActionResult> GetById(string id)
    {
        var resultado = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<object>.Ok(resultado));
    }

    /// <summary>
    /// Crea un nuevo usuario en el sistema. (Solo soporte)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "soporte")]
    public async Task<IActionResult> Create([FromBody] CreateUsuarioRequest request)
    {
        var creado = await _service.CreateAsync(request);
        return StatusCode(201, ApiResponse<object>.Ok(creado, "Usuario creado exitosamente"));
    }

    /// <summary>
    /// Actualiza los datos de un usuario por su ID. (Solo soporte)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "soporte")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUsuarioRequest request)
    {
        await _service.UpdateAsync(id, request);
        return Ok(ApiResponse<object>.Ok(null!, "Usuario actualizado exitosamente"));
    }

    /// <summary>
    /// Cambia la contraseña de cualquier usuario sin requerir la anterior. (Solo soporte)
    /// </summary>
    [HttpPatch("{id}/password")]
    [Authorize(Roles = "soporte")]
    public async Task<IActionResult> AdminCambiarPassword(string id, [FromBody] AdminCambiarPasswordRequest request)
    {
        await _service.AdminCambiarPasswordAsync(id, request);
        return Ok(ApiResponse<object>.Ok(null!, "Contraseña actualizada exitosamente"));
    }

    /// <summary>
    /// Elimina un usuario por su ID. (Solo soporte)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "soporte")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, "Usuario eliminado exitosamente"));
    }

    /// <summary>
    /// Permite al usuario autenticado editar su propio perfil (nombre y correo).
    /// </summary>
    [HttpPut("mi-perfil")]
    public async Task<IActionResult> UpdateMiPerfil([FromBody] UpdateMiPerfilRequest request)
    {
        var idActual = ObtenerIdDelToken();
        var usuarioActualizado = await _service.UpdateMiPerfilAsync(idActual, request);
        return Ok(ApiResponse<UsuarioResponse>.Ok(usuarioActualizado, "Perfil actualizado exitosamente"));
    }

    /// <summary>
    /// Permite al usuario autenticado cambiar su propia contraseña.
    /// </summary>
    [HttpPatch("mi-password")]
    public async Task<IActionResult> CambiarMiPassword([FromBody] CambiarMiPasswordRequest request)
    {
        var idActual = ObtenerIdDelToken();
        await _service.CambiarMiPasswordAsync(idActual, request);
        return Ok(ApiResponse<object>.Ok(null!, "Contraseña cambiada exitosamente"));
    }

    private string ObtenerIdDelToken()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier)
              ?? User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(id))
            throw new Application.Common.DomainException(
                Application.Common.ApiErrors.Unauthorized,
                "No se pudo identificar al usuario desde el token.");

        return id;
    }
}
