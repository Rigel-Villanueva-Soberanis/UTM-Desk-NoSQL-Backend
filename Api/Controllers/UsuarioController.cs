using Api.Common;
using Application.Common.ErrorCatalog;
using Application.Common.Seguridad;
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

    /// <remarks>
    /// Lista las cuentas del sistema con paginación y filtros.
    ///
    /// **Roles:** Soporte.
    ///
    /// - `busqueda`: busca a la vez en nombres, apellidos, usuario y correo.
    /// - `nombres` / `apellidos`: búsqueda parcial (`apellidos` revisa paterno y materno).
    /// - `rol`, `clasificacion`: valor exacto (ej. `técnico`, `redes`).
    /// - `estadoCuenta`: `activo`, `inactivo` o `bloqueado`.
    ///
    /// Los textos no distinguen mayúsculas ni acentos (`jose` encuentra `José`).
    /// Ordenado por nombres y apellido paterno. Máximo 50 por página.
    ///
    /// </remarks>
    /// <response code="200">Página de usuarios.</response>
    /// <response code="400">Filtros inválidos.</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="403">El rol no puede ver las cuentas.</response>
    [HttpGet]
    [Authorize(Roles = RolesSistema.Soporte)]
    public async Task<IActionResult> GetAll([FromQuery] UsuarioFiltroRequest filtro)
    {
        var resultado = await _service.GetAllAsync(filtro);
        return Ok(ApiResponse<PaginadoResponse<UsuarioResponse>>.Ok(resultado));
    }

    /// <remarks>
    /// Lista los técnicos disponibles para asignar una incidencia.
    ///
    /// **Roles:** Root, Soporte, Administrativos.
    ///
    /// Solo devuelve técnicos con cuenta `activo`, con datos mínimos:
    /// `id` (el que se manda en `PATCH /api/incidencias/{id}/asignar-tecnico`), `nombreCompleto`,
    /// `clasificacion` (especialidad) e `incidenciasActivas` (asignadas que no están resueltas ni canceladas).
    ///
    /// Filtros opcionales: `busqueda` (nombres y apellidos), `nombres`, `apellidos`, `clasificacion`.
    /// Sin paginación. Ordenado de menor a mayor carga de trabajo.
    ///
    /// </remarks>
    /// <response code="200">Lista de técnicos.</response>
    /// <response code="400">Filtros inválidos.</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="403">El rol no puede asignar técnicos.</response>
    [HttpGet("tecnicos")]
    [Authorize(Roles = RolesSistema.Root + "," + RolesSistema.Soporte + "," + RolesSistema.Administrativos)]
    public async Task<IActionResult> GetTecnicos([FromQuery] TecnicoFiltroRequest filtro)
    {
        var resultado = await _service.GetTecnicosAsync(filtro);
        return Ok(ApiResponse<List<TecnicoDisponibleResponse>>.Ok(resultado));
    }

    /// <remarks>
    /// Obtiene un usuario por su ID de MongoDB. (Solo soporte)
    /// </remarks>
    [HttpGet("{id}")]
    [Authorize(Roles = "soporte")]
    public async Task<IActionResult> GetById(string id)
    {
        var resultado = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<object>.Ok(resultado));
    }

    /// <remarks>
    /// Crea un nuevo usuario en el sistema. (Solo soporte)
    /// </remarks>
    [HttpPost]
    [Authorize(Roles = "soporte")]
    public async Task<IActionResult> Create([FromBody] CreateUsuarioRequest request)
    {
        var creado = await _service.CreateAsync(request);
        return StatusCode(201, ApiResponse<object>.Ok(creado, "Usuario creado exitosamente"));
    }

    /// <remarks>
    /// Actualiza los datos de un usuario por su ID. (Solo soporte)
    /// </remarks>
    [HttpPut("{id}")]
    [Authorize(Roles = "soporte")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUsuarioRequest request)
    {
        await _service.UpdateAsync(id, request);
        return Ok(ApiResponse<object>.Ok(null!, "Usuario actualizado exitosamente"));
    }

    /// <remarks>
    /// Cambia la contraseña de cualquier usuario sin requerir la anterior. (Solo soporte)
    /// </remarks>
    [HttpPatch("{id}/password")]
    [Authorize(Roles = "soporte")]
    public async Task<IActionResult> AdminCambiarPassword(string id, [FromBody] AdminCambiarPasswordRequest request)
    {
        await _service.AdminCambiarPasswordAsync(id, request);
        return Ok(ApiResponse<object>.Ok(null!, "Contraseña actualizada exitosamente"));
    }

    /// <remarks>
    /// Elimina un usuario por su ID. (Solo soporte)
    /// </remarks>
    [HttpDelete("{id}")]
    [Authorize(Roles = "soporte")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null!, "Usuario eliminado exitosamente"));
    }

    /// <remarks>
    /// Devuelve los datos de la cuenta de quien tiene la sesión iniciada.
    ///
    /// **Roles:** cualquier usuario autenticado.
    ///
    /// No recibe parámetros: el usuario se toma del token. Nunca incluye la contraseña.
    ///
    /// </remarks>
    /// <response code="200">Datos del perfil.</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="404">La cuenta ya no existe.</response>
    [HttpGet("mi-perfil")]
    public async Task<IActionResult> GetMiPerfil()
    {
        var resultado = await _service.GetMiPerfilAsync(ObtenerIdDelToken());
        return Ok(ApiResponse<UsuarioResponse>.Ok(resultado));
    }

    /// <remarks>
    /// Permite al usuario autenticado editar su propio perfil (nombre y correo).
    /// </remarks>
    [HttpPut("mi-perfil")]
    public async Task<IActionResult> UpdateMiPerfil([FromBody] UpdateMiPerfilRequest request)
    {
        var idActual = ObtenerIdDelToken();
        var usuarioActualizado = await _service.UpdateMiPerfilAsync(idActual, request);
        return Ok(ApiResponse<UsuarioResponse>.Ok(usuarioActualizado, "Perfil actualizado exitosamente"));
    }

    /// <remarks>
    /// Permite al usuario autenticado cambiar su propia contraseña.
    /// </remarks>
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
            throw new DomainException(
                ApiErrors.Unauthorized,
                "No se pudo identificar al usuario desde el token.");

        return id;
    }
}
