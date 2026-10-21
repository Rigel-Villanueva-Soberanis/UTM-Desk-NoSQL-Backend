using Api.Common;
using Application.DTOs.Requests;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogoController : ControllerBase
{
    private readonly CatalogoService _service;

    public CatalogoController(CatalogoService service)
    {
        _service = service;
    }

    /// <summary>
    /// Obtiene todos los catálogos del sistema.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var resultado = await _service.GetAllAsync();
        return Ok(ApiResponse<object>.Ok(resultado));
    }

    /// <summary>
    /// Obtiene un catálogo por su ID de MongoDB.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var resultado = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<object>.Ok(resultado));
    }

    /// <summary>
    /// Obtiene un catálogo por su clave semántica. Ej: /api/catalogo/clave/estatus_incidencia
    /// </summary>
    [HttpGet("clave/{clave}")]
    public async Task<IActionResult> GetByClave(string clave)
    {
        var resultado = await _service.GetByClaveAsync(clave);
        return Ok(ApiResponse<object>.Ok(resultado));
    }

    /// <summary>
    /// Crea un nuevo catálogo. El ID lo genera MongoDB automáticamente.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCatalogoRequest request)
    {
        var creado = await _service.CreateAsync(request);
        return StatusCode(201, ApiResponse<object>.Ok(creado, "Catálogo creado exitosamente"));
    }

    /// <summary>
    /// Actualiza completamente un catálogo existente por ID.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCatalogoRequest request)
    {
        await _service.UpdateAsync(id, request);
        return Ok(ApiResponse<object>.Ok(null, "Catálogo actualizado exitosamente"));
    }

    /// <summary>
    /// Elimina un catálogo por su ID.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return Ok(ApiResponse<object>.Ok(null, "Catálogo eliminado exitosamente"));
    }
}
