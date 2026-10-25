using Api.Common;
using Api.Security;
using Application.Common;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Incidencias (tickets) — Fase 1: registro con folio atómico y consultas base.
/// </summary>
[ApiController]
[Route("api/incidencias")]
[Produces("application/json")]
[Authorize]
public class IncidenciaController : ControllerBase
{
    private readonly IncidenciaService _service;

    public IncidenciaController(IncidenciaService service)
    {
        _service = service;
    }

    /// <summary>
    /// Registra una nueva incidencia.
    /// </summary>
    /// <remarks>
    /// **Roles:** Root, Reportante.
    ///
    /// El backend genera el folio (INC-AAAA-XXXX) con Redis, toma los datos del reportante del token
    /// y la inicializa en estado `abierta` con `fechas.resolucion = null`.
    /// `categoria` y `claveUbicacion` deben existir en los catálogos; `detalleEspecifico` debe cumplir
    /// los campos dinámicos de la categoría.
    /// </remarks>
    /// <response code="201">Incidencia registrada.</response>
    /// <response code="400">Datos inválidos (lista de errores por campo).</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="403">El rol no puede registrar incidencias.</response>
    /// <response code="503">No se pudo generar el folio (Redis no disponible).</response>
    [HttpPost]
    [Authorize(Roles = RolesSistema.Root + "," + RolesSistema.Reportante)]
    public async Task<IActionResult> Create([FromBody] CreateIncidenciaRequest request)
    {
        var creada = await _service.CreateAsync(request, User.ObtenerUsuarioActual());
        return StatusCode(201, ApiResponse<IncidenciaResponse>.Ok(creada, "Incidencia registrada exitosamente"));
    }

    /// <summary>
    /// Lista incidencias con paginación y filtros.
    /// </summary>
    /// <remarks>
    /// **Roles:** todos los autenticados.
    ///
    /// - **Reportante:** solo ve sus propias incidencias (el filtro se aplica en el backend).
    /// - **Técnico:** ve la cola global de trabajo.
    /// - **Demás roles:** lista global.
    ///
    /// Ordenado por fecha de creación, más recientes primero.
    /// </remarks>
    /// <response code="200">Página de incidencias.</response>
    /// <response code="400">Filtros inválidos.</response>
    /// <response code="401">Sin token o token inválido.</response>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] IncidenciaFiltroRequest filtro)
    {
        var resultado = await _service.GetAllAsync(filtro, User.ObtenerUsuarioActual());
        return Ok(ApiResponse<PaginadoResponse<IncidenciaResumenResponse>>.Ok(resultado));
    }

    /// <summary>
    /// Obtiene el expediente completo de una incidencia por su id de MongoDB.
    /// </summary>
    /// <remarks>
    /// **Roles:** todos los autenticados.
    ///
    /// - **Reportante:** solo si él la creó.
    /// - **Técnico y demás roles:** cualquier incidencia.
    /// </remarks>
    /// <param name="id">_id de la incidencia. Ej: 6add4fd725d0728c74d86f2e</param>
    /// <response code="200">Expediente completo.</response>
    /// <response code="400">El id no tiene formato válido.</response>
    /// <response code="403">No tiene acceso a esta incidencia.</response>
    /// <response code="404">La incidencia no existe.</response>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var resultado = await _service.GetByIdAsync(id, User.ObtenerUsuarioActual());
        return Ok(ApiResponse<IncidenciaResponse>.Ok(resultado));
    }
}
