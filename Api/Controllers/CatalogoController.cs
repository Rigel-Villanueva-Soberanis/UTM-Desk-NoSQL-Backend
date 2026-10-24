using Api.Common;
using Application.DTOs.Requests;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Api.Controllers;

/// <summary>
/// Endpoint único para todos los catálogos del sistema.
/// Rutas: /api/catalogos/{claveCatalogo}/elementos/{claveElemento}
///   • claveCatalogo → campo "clave_catalogo" del documento (ej. "edificios", "roles_sistema").
///   • claveElemento → identificador de una fila dentro de "contenido"
///                     ("clave" en edificios y categorías; "rol" en roles_sistema).
/// GET: cualquier rol autenticado. PUT / POST / PATCH: depende del catálogo (ver Application/Validators/Catalogos).
/// No se pueden crear ni borrar catálogos completos.
/// </summary>
[ApiController]
[Route("api/catalogos")]
[Produces("application/json")]
[Authorize]
public class CatalogoController : ControllerBase
{
    private readonly CatalogoService _service;

    public CatalogoController(CatalogoService service)
    {
        _service = service;
    }

    // ─── Catálogo completo ──────────────────────────────────────────────────

    /// <summary>
    /// Obtiene todos los catálogos. (Todos los roles)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var resultado = await _service.GetAllAsync();
        return Ok(ApiResponse<object>.Ok(resultado));
    }

    /// <summary>
    /// Obtiene un catálogo completo por su clave. Ej: /api/catalogos/edificios (Todos los roles)
    /// </summary>
    [HttpGet("{claveCatalogo}")]
    public async Task<IActionResult> GetByClave(string claveCatalogo)
    {
        var resultado = await _service.GetByClaveAsync(claveCatalogo);
        return Ok(ApiResponse<object>.Ok(resultado));
    }

    /// <summary>
    /// Reemplaza TODO el contenido de un catálogo.
    /// roles_sistema: Root, Administrativos, Soporte · edificios: Root, Administrativos, Soporte ·
    /// categorias_incidencia: Root, Soporte.
    /// </summary>
    [HttpPut("{claveCatalogo}")]
    public async Task<IActionResult> Update(string claveCatalogo, [FromBody] UpdateCatalogoRequest request)
    {
        var actualizado = await _service.UpdateAsync(claveCatalogo, request, ObtenerRol());
        return Ok(ApiResponse<object>.Ok(actualizado, "Catálogo actualizado exitosamente"));
    }

    // ─── Elementos individuales ─────────────────────────────────────────────

    /// <summary>
    /// Obtiene un solo elemento. Ej: /api/catalogos/edificios/elementos/lab_c3 (Todos los roles)
    /// </summary>
    [HttpGet("{claveCatalogo}/elementos/{claveElemento}")]
    public async Task<IActionResult> GetElemento(string claveCatalogo, string claveElemento)
    {
        var resultado = await _service.GetElementoAsync(claveCatalogo, claveElemento);
        return Ok(ApiResponse<object>.Ok(resultado!));
    }

    /// <summary>
    /// Agrega un elemento al catálogo. Body plano, ej. edificios:
    /// { "clave": "aula_a1", "edificio": "Edificio A", "salon": "Aula 1" }
    /// (roles_sistema no permite agregar porque sus roles son fijos)
    /// </summary>
    [HttpPost("{claveCatalogo}/elementos")]
    public async Task<IActionResult> AgregarElemento(string claveCatalogo, [FromBody] ElementoCatalogoRequest request)
    {
        var creado = await _service.AgregarElementoAsync(claveCatalogo, request, ObtenerRol());
        return StatusCode(201, ApiResponse<object>.Ok(creado!, "Elemento agregado exitosamente"));
    }

    /// <summary>
    /// Actualiza solo los campos enviados de un elemento. Ej. PATCH /api/catalogos/edificios/elementos/aula_a1
    /// { "salon": "Aula 1 Planta Alta" }  → edificio y clave se conservan.
    /// </summary>
    [HttpPatch("{claveCatalogo}/elementos/{claveElemento}")]
    public async Task<IActionResult> ActualizarElemento(string claveCatalogo, string claveElemento, [FromBody] ElementoCatalogoRequest request)
    {
        var actualizado = await _service.ActualizarElementoAsync(claveCatalogo, claveElemento, request, ObtenerRol());
        return Ok(ApiResponse<object>.Ok(actualizado!, "Elemento actualizado exitosamente"));
    }

    private string ObtenerRol()
        => User.FindFirstValue(ClaimTypes.Role)
        ?? User.FindFirstValue("rol_sistema")
        ?? string.Empty;
}
