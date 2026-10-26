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

    /// <remarks>
    /// Obtiene todos los catálogos. (Todos los roles)
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var resultado = await _service.GetAllAsync();
        return Ok(ApiResponse<object>.Ok(resultado));
    }

    /// <remarks>
    /// Obtiene un catálogo completo por su clave. Ej: /api/catalogos/edificios (Todos los roles)
    /// </remarks>
    [HttpGet("{claveCatalogo}")]
    public async Task<IActionResult> GetByClave(string claveCatalogo)
    {
        var resultado = await _service.GetByClaveAsync(claveCatalogo);
        return Ok(ApiResponse<object>.Ok(resultado));
    }

    /// <remarks>
    /// Reemplaza TODO el contenido de un catálogo.
    /// roles_sistema, edificios y categorias_incidencia: Root y Soporte de TI.
    /// </remarks>
    [HttpPut("{claveCatalogo}")]
    public async Task<IActionResult> Update(string claveCatalogo, [FromBody] UpdateCatalogoRequest request)
    {
        var actualizado = await _service.UpdateAsync(claveCatalogo, request, ObtenerRol());
        return Ok(ApiResponse<object>.Ok(actualizado, "Catálogo actualizado exitosamente"));
    }

    // ─── Elementos individuales ─────────────────────────────────────────────

    /// <remarks>
    /// Obtiene un solo elemento. Ej: /api/catalogos/edificios/elementos/lab_c3 (Todos los roles)
    /// </remarks>
    [HttpGet("{claveCatalogo}/elementos/{claveElemento}")]
    public async Task<IActionResult> GetElemento(string claveCatalogo, string claveElemento)
    {
        var resultado = await _service.GetElementoAsync(claveCatalogo, claveElemento);
        return Ok(ApiResponse<object>.Ok(resultado!));
    }

    /// <remarks>
    /// Agrega un elemento al catálogo. Body plano, ej. edificios:
    /// { "clave": "edificio_a", "edificio": "Edificio A", "salones": ["Aula 101", "Aula 102"] }
    /// (`salones` es opcional; la clave y el nombre del edificio no pueden repetirse)
    /// (roles_sistema no permite agregar porque sus roles son fijos)
    /// </remarks>
    [HttpPost("{claveCatalogo}/elementos")]
    public async Task<IActionResult> AgregarElemento(string claveCatalogo, [FromBody] ElementoCatalogoRequest request)
    {
        var creado = await _service.AgregarElementoAsync(claveCatalogo, request, ObtenerRol());
        return StatusCode(201, ApiResponse<object>.Ok(creado!, "Elemento agregado exitosamente"));
    }

    /// <remarks>
    /// Actualiza solo los campos enviados de un elemento. Ej. PATCH /api/catalogos/edificios/elementos/edificio_a
    /// { "edificio": "Edificio A (Docencia)" }  → clave y salones se conservan.
    /// Si se envía `salones` en un edificio, REEMPLAZA la lista completa; para agregar o renombrar un salón usa
    /// los endpoints de salones.
    /// </remarks>
    [HttpPatch("{claveCatalogo}/elementos/{claveElemento}")]
    public async Task<IActionResult> ActualizarElemento(string claveCatalogo, string claveElemento, [FromBody] ElementoCatalogoRequest request)
    {
        var actualizado = await _service.ActualizarElementoAsync(claveCatalogo, claveElemento, request, ObtenerRol());
        return Ok(ApiResponse<object>.Ok(actualizado!, "Elemento actualizado exitosamente"));
    }

    // ─── Salones de un edificio ─────────────────────────────────────────────

    /// <remarks>
    /// Agrega salones a un edificio **sin mandar la lista completa**.
    ///
    /// **Roles:** Root, Soporte de TI.
    ///
    /// - `claveEdificio`: la clave del edificio (ej. `edificio_a`).
    /// - Los nombres nuevos se suman a los que ya tiene; no pueden repetirse entre sí ni con los existentes
    ///   (sin distinguir mayúsculas ni acentos). Máximo 100 salones por edificio.
    ///
    /// Responde el edificio completo con su lista de salones actualizada.
    ///
    /// </remarks>
    /// <param name="claveEdificio">Clave del edificio.</param>
    /// <param name="request">Salones a agregar.</param>
    /// <response code="200">Salones agregados; devuelve el edificio completo.</response>
    /// <response code="400">Salones inválidos o repetidos (lista de errores por campo).</response>
    /// <response code="403">El rol no puede modificar los edificios.</response>
    /// <response code="404">El edificio no existe.</response>
    [HttpPatch("edificios/elementos/{claveEdificio}/salones")]
    public async Task<IActionResult> AgregarSalones(string claveEdificio, [FromBody] AgregarSalonesRequest request)
    {
        var actualizado = await _service.AgregarSalonesAsync(claveEdificio, request, ObtenerRol());
        return Ok(ApiResponse<object>.Ok(actualizado!, "Salones agregados exitosamente"));
    }

    /// <remarks>
    /// Cambia el nombre de **un** salón de un edificio.
    ///
    /// **Roles:** Root, Soporte de TI.
    ///
    /// - `claveEdificio`: la clave del edificio (ej. `edificio_a`).
    /// - `salon` (en la ruta): el nombre actual del salón, sin distinguir mayúsculas ni acentos. Si tiene espacios, va codificado (`Aula%20104`).
    /// - Body: `{ "salon": "Aula 104 Planta Alta" }`.
    ///
    /// Las incidencias ya registradas conservan el nombre con el que se reportaron.
    ///
    /// </remarks>
    /// <param name="claveEdificio">Clave del edificio.</param>
    /// <param name="salon">Nombre actual del salón.</param>
    /// <param name="request">Nombre nuevo.</param>
    /// <response code="200">Salón actualizado; devuelve el edificio completo.</response>
    /// <response code="400">Nombre inválido o ya existe en el edificio.</response>
    /// <response code="403">El rol no puede modificar los edificios.</response>
    /// <response code="404">El edificio o el salón no existen.</response>
    [HttpPatch("edificios/elementos/{claveEdificio}/salones/{salon}")]
    public async Task<IActionResult> ActualizarSalon(string claveEdificio, string salon, [FromBody] ActualizarSalonRequest request)
    {
        var actualizado = await _service.ActualizarSalonAsync(claveEdificio, salon, request, ObtenerRol());
        return Ok(ApiResponse<object>.Ok(actualizado!, "Salón actualizado exitosamente"));
    }

    private string ObtenerRol()
        => User.FindFirstValue(ClaimTypes.Role)
        ?? User.FindFirstValue("rol_sistema")
        ?? string.Empty;
}
