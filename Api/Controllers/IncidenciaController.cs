using Api.Common;
using Api.Security;
using Application.Common.Seguridad;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Incidencias (tickets) — Fase 1: registro y consultas · Fase 2: evidencias · Fase 3: gestión y bitácora · Fase 4: etiquetas, proveedor externo, edición y borrado.
/// </summary>
[ApiController]
[Route("api/incidencias")]
[Produces("application/json")]
[Authorize]
public class IncidenciaController : ControllerBase
{
    /// <summary>Roles que administran el flujo de trabajo (asignar, cambiar cualquier estado).</summary>
    private const string RolesGestion = RolesSistema.Root + "," + RolesSistema.Soporte + "," + RolesSistema.Administrativos;

    /// <summary>Valores del rol técnico en el token (con y sin acento).</summary>
    private const string RolesTecnico = RolesSistema.TecnicoToken + "," + RolesSistema.Tecnico;

    private readonly IncidenciaService _service;
    private readonly GestionIncidenciaService _gestionService;
    private readonly AdministracionIncidenciaService _adminService;

    public IncidenciaController(
        IncidenciaService service,
        GestionIncidenciaService gestionService,
        AdministracionIncidenciaService adminService)
    {
        _service        = service;
        _gestionService = gestionService;
        _adminService   = adminService;
    }

    /// <remarks>
    /// Registra una nueva incidencia.
    ///
    /// **Roles:** Root, Reportante.
    ///
    /// El backend genera el folio (INC-AAAA-XXXX) con Redis, toma los datos del reportante del token
    /// y la inicializa en estado `abierta` con `fechas.resolucion = null`.
    /// Las imágenes se agregan después: `POST /api/evidencias/upload` y luego `PATCH /api/incidencias/{id}/evidencias`.
    /// `categoria` y `claveUbicacion` deben existir en los catálogos; `detalleEspecifico` debe cumplir
    /// los campos dinámicos de la categoría.
    ///
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

    /// <remarks>
    /// Lista incidencias con paginación y filtros.
    ///
    /// **Roles:** todos los autenticados.
    ///
    /// - **Reportante:** solo ve sus propias incidencias (el filtro se aplica en el backend).
    /// - **Técnico:** solo ve las incidencias que tiene asignadas.
    /// - **Demás roles:** lista global.
    ///
    /// Filtros (todos opcionales y combinables):
    ///
    /// - `estado`, `prioridad`, `categoria`, `edificio`.
    /// - `asignada`: `true` = con técnico, `false` = sin asignar (bandeja para asignar), vacío = todas.
    /// - `tag`: incidencias con esa etiqueta (sin importar mayúsculas ni acentos).
    /// - `conProveedorExterno`: `true` = canalizadas con proveedor externo, `false` = sin proveedor.
    /// - `fechaDesde` / `fechaHasta`: rango de fecha de creación (AAAA-MM-DD, hora de México, ambos días incluidos).
    ///
    /// Cada fila trae `asignada`, `idTecnico` y `nombreTecnico`.
    ///
    /// Ordenado por fecha de creación, más recientes primero.
    ///
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

    /// <remarks>
    /// Obtiene el expediente completo de una incidencia por su id de MongoDB.
    ///
    /// **Roles:** todos los autenticados.
    ///
    /// - **Reportante:** solo si él la creó.
    /// - **Técnico:** solo si está asignado a ella.
    /// - **Demás roles:** cualquier incidencia.
    ///
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

    /// <remarks>
    /// Liga a la incidencia de 1 a 3 archivos ya subidos con `POST /api/evidencias/upload` ($push en `evidencias_adjuntas`).
    ///
    /// **Roles:**
    ///
    /// - **Root:** en cualquier incidencia.
    /// - **Reportante:** solo en las suyas (evidencia inicial).
    /// - **Técnico:** solo en las que tiene asignadas (evidencia de reparación).
    ///
    /// El backend agrega los datos del usuario (del token) y la fecha actual, y registra la acción en la bitácora.
    /// No se permite en incidencias `resuelta` o `cancelada`. No se puede ligar dos veces el mismo archivo.
    ///
    /// **Límite por incidencia:** Reportante máximo 3 evidencias suyas · Técnico máximo 3 suyas · Root sin límite.
    /// </remarks>
    /// <param name="id">_id de la incidencia.</param>
    /// <param name="request">Datos que devolvió el upload, en la lista `evidencias`.</param>
    /// <response code="200">Evidencias agregadas.</response>
    /// <response code="400">Datos inválidos, archivo ajeno o repetido.</response>
    /// <response code="403">No tiene permiso sobre esta incidencia.</response>
    /// <response code="404">La incidencia no existe.</response>
    /// <response code="409">La incidencia está cerrada o el rol ya llegó a su límite de evidencias.</response>
    [HttpPatch("{id}/evidencias")]
    [Authorize(Roles = RolesSistema.Root + "," + RolesSistema.Reportante + "," + RolesSistema.TecnicoToken + "," + RolesSistema.Tecnico)]
    public async Task<IActionResult> AgregarEvidencias(string id, [FromBody] AgregarEvidenciasRequest request)
    {
        var evidencias = await _service.AgregarEvidenciasAsync(id, request, User.ObtenerUsuarioActual());
        return Ok(ApiResponse<List<EvidenciaResponse>>.Ok(evidencias, "Evidencias agregadas exitosamente"));
    }

    // ─── Fase 3: gestión del ticket (todas inyectan su registro en la bitácora) ──

    /// <remarks>
    /// Asigna o reasigna el técnico responsable.
    ///
    /// **Roles:** Root, Soporte de TI, Administrativos.
    ///
    /// - Verifica en la base de datos que el usuario tenga **rol Técnico** y la cuenta activa; si es docente, alumno o administrativo se rechaza.
    /// - Si no tenía técnico lo vincula; si ya tenía, lo **reemplaza**.
    /// - Si estaba `abierta`, pasa automáticamente a `en proceso`.
    /// - **Bitácora:** asignación inicial o reasignación con el nombre del técnico (y el cambio automático de estado).
    /// </remarks>
    /// <param name="id">_id de la incidencia.</param>
    /// <param name="request">Técnico a asignar.</param>
    /// <response code="200">Incidencia actualizada.</response>
    /// <response code="400">Id inválido, el usuario no es técnico o su cuenta no está activa.</response>
    /// <response code="403">El rol no puede asignar técnicos.</response>
    /// <response code="404">La incidencia o el técnico no existen.</response>
    /// <response code="409">Ya es el técnico asignado, o la incidencia está cerrada.</response>
    [HttpPatch("{id}/asignar-tecnico")]
    [Authorize(Roles = RolesGestion)]
    public async Task<IActionResult> AsignarTecnico(string id, [FromBody] AsignarTecnicoRequest request)
    {
        var resultado = await _gestionService.AsignarTecnicoAsync(id, request, User.ObtenerUsuarioActual());
        return Ok(ApiResponse<IncidenciaResponse>.Ok(resultado, "Técnico asignado exitosamente"));
    }

    /// <remarks>
    /// Cambia el estado de la incidencia con su motivo.
    ///
    /// **Roles:**
    ///
    /// - **Root, Soporte de TI, Administrativos:** cualquier cambio, incluida la reapertura `resuelta` → `en proceso`.
    /// - **Técnico:** solo en sus incidencias asignadas: `abierta` → `en proceso` y `en proceso` → `resuelta`.
    ///
    /// Flujo normal: `abierta` → `en proceso` → `resuelta` / `cancelada`.
    ///
    /// - **`resuelta`:** guarda `fechas.resolucion` = ahora; empieza el conteo de 60 días del índice TTL.
    /// - **Reapertura:** `fechas.resolucion` = null; el TTL se detiene y el documento ya no se borra.
    /// - **Bitácora:** `estado_anterior` → `estado_reportado` con el motivo.
    /// </remarks>
    /// <param name="id">_id de la incidencia.</param>
    /// <param name="request">Nuevo estado y motivo.</param>
    /// <response code="200">Incidencia actualizada.</response>
    /// <response code="400">Estado inválido, igual al actual o sin motivo.</response>
    /// <response code="403">El técnico no está asignado o no puede pasar a ese estado.</response>
    /// <response code="404">La incidencia no existe.</response>
    /// <response code="409">Cambio de estado no permitido, o la incidencia cambió al mismo tiempo.</response>
    [HttpPatch("{id}/cambiar-estado")]
    [Authorize(Roles = RolesGestion + "," + RolesTecnico)]
    public async Task<IActionResult> CambiarEstado(string id, [FromBody] CambiarEstadoRequest request)
    {
        var resultado = await _gestionService.CambiarEstadoAsync(id, request, User.ObtenerUsuarioActual());
        return Ok(ApiResponse<IncidenciaResponse>.Ok(resultado, "Estado actualizado exitosamente"));
    }

    /// <remarks>
    /// Agrega una nota de texto libre a la bitácora (avance, diagnóstico, notas internas).
    ///
    /// **Roles:** Root, Técnico (asignado a la incidencia).
    ///
    /// No cambia el estado ni los datos del ticket. Solo texto: las fotos van por el endpoint de evidencias.
    /// Se guarda con `$push` en `historial_seguimiento` con la fecha y la firma del usuario.
    /// </remarks>
    /// <param name="id">_id de la incidencia.</param>
    /// <param name="request">Texto de la nota.</param>
    /// <response code="200">Nota registrada.</response>
    /// <response code="400">Nota vacía o muy larga.</response>
    /// <response code="403">No es el técnico asignado.</response>
    /// <response code="404">La incidencia no existe.</response>
    /// <response code="409">La incidencia está cerrada.</response>
    [HttpPatch("{id}/seguimiento")]
    [Authorize(Roles = RolesSistema.Root + "," + RolesTecnico)]
    public async Task<IActionResult> AgregarSeguimiento(string id, [FromBody] NotaSeguimientoRequest request)
    {
        var nota = await _gestionService.AgregarNotaAsync(id, request, User.ObtenerUsuarioActual());
        return Ok(ApiResponse<SeguimientoResponse>.Ok(nota, "Nota registrada exitosamente"));
    }

    /// <remarks>
    /// Suma horas trabajadas al total de la incidencia.
    ///
    /// **Roles:** Root, Técnico (asignado a la incidencia).
    ///
    /// Usa `$inc`: si tenía 1.5 h y se registran 2 h, el total queda en 3.5 h.
    /// **Bitácora:** registra las horas sumadas en la sesión.
    /// </remarks>
    /// <param name="id">_id de la incidencia.</param>
    /// <param name="request">Horas de la sesión (0.25 a 24) y comentario opcional.</param>
    /// <response code="200">Horas registradas; incluye el nuevo total.</response>
    /// <response code="400">Horas fuera de rango.</response>
    /// <response code="403">No es el técnico asignado.</response>
    /// <response code="404">La incidencia no existe.</response>
    /// <response code="409">La incidencia está cerrada.</response>
    [HttpPatch("{id}/horas-invertidas")]
    [Authorize(Roles = RolesSistema.Root + "," + RolesTecnico)]
    public async Task<IActionResult> RegistrarHoras(string id, [FromBody] RegistrarHorasRequest request)
    {
        var resultado = await _gestionService.RegistrarHorasAsync(id, request, User.ObtenerUsuarioActual());
        return Ok(ApiResponse<HorasInvertidasResponse>.Ok(resultado, "Horas registradas exitosamente"));
    }

    // ─── Fase 4: etiquetas, proveedor externo, edición y borrado ──────────────

    /// <remarks>
    /// Devuelve todas las etiquetas distintas usadas en las incidencias, en orden alfabético.
    ///
    /// **Roles:** todos los autenticados.
    ///
    /// Usa `distinct` sobre el arreglo `tags` en MongoDB. Pensado para autocompletar al redactar un reporte o al filtrar.
    /// Con `busqueda` devuelve solo las que contienen ese texto (sin importar mayúsculas ni acentos).
    ///
    /// Ejemplo de respuesta: `["clima", "proyector", "redes", "soporte-laptop"]`
    /// </remarks>
    /// <param name="busqueda">Texto opcional para filtrar. Ej: proy</param>
    /// <response code="200">Lista de etiquetas.</response>
    /// <response code="400">La búsqueda trae caracteres no permitidos.</response>
    /// <response code="401">Sin token o token inválido.</response>
    [HttpGet("tags-sugeridos")]
    public async Task<IActionResult> GetTagsSugeridos([FromQuery] string? busqueda)
    {
        var tags = await _adminService.GetTagsSugeridosAsync(busqueda);
        return Ok(ApiResponse<List<string>>.Ok(tags));
    }

    /// <remarks>
    /// Agrega y/o quita etiquetas de la incidencia. Devuelve la lista final de etiquetas.
    ///
    /// **Roles:** Root, Técnico (solo en sus incidencias asignadas), Soporte de TI, Administrativos.
    ///
    /// - Las etiquetas se guardan en minúsculas y sin acentos: `"Proyector Eléctrico"` → `"proyector electrico"`.
    /// - `agregar` usa `$addToSet`: si la etiqueta ya está en el ticket no se duplica.
    /// - `quitar` usa `$pull`.
    /// - Solo letras, números, espacios, guiones y guiones bajos; de 2 a 30 caracteres. Máximo 10 por lista y 15 por incidencia.
    /// - **Bitácora:** registra las etiquetas agregadas y quitadas.
    ///
    /// Ejemplo: `{ "agregar": ["Proyector", "cable hdmi"], "quitar": ["clima"] }`
    /// </remarks>
    /// <param name="id">_id de la incidencia.</param>
    /// <param name="request">Etiquetas a agregar y/o quitar.</param>
    /// <response code="200">Etiquetas actualizadas.</response>
    /// <response code="400">Etiquetas inválidas o listas vacías.</response>
    /// <response code="403">El rol no puede editar etiquetas, o el técnico no está asignado.</response>
    /// <response code="404">La incidencia no existe.</response>
    /// <response code="409">Se pasaría del máximo de etiquetas por incidencia.</response>
    [HttpPatch("{id}/tags")]
    [Authorize(Roles = RolesGestion + "," + RolesTecnico)]
    public async Task<IActionResult> ActualizarTags(string id, [FromBody] ActualizarTagsRequest request)
    {
        var tags = await _adminService.ActualizarTagsAsync(id, request, User.ObtenerUsuarioActual());
        return Ok(ApiResponse<List<string>>.Ok(tags, "Etiquetas actualizadas exitosamente"));
    }

    /// <remarks>
    /// Canaliza la incidencia con un proveedor externo (subcontratación).
    ///
    /// **Roles:** Root, Soporte de TI, Administrativos.
    ///
    /// - Guarda el subdocumento `tecnico_externo` con `requiere_servicio_externo = true`, empresa, técnico responsable,
    ///   contacto y orden de servicio. Si ya tenía proveedor, lo **reemplaza**.
    /// - **Bitácora:** "Incidencia canalizada con proveedor externo: Telmex. Orden de servicio: OS-8841."
    /// - No se permite en incidencias `resuelta` o `cancelada`.
    /// </remarks>
    /// <param name="id">_id de la incidencia.</param>
    /// <param name="request">Datos del proveedor externo.</param>
    /// <response code="200">Incidencia actualizada.</response>
    /// <response code="400">Datos inválidos.</response>
    /// <response code="403">El rol no puede canalizar con proveedores.</response>
    /// <response code="404">La incidencia no existe.</response>
    /// <response code="409">La incidencia está cerrada, ya tiene ese mismo proveedor, o cambió al mismo tiempo.</response>
    [HttpPatch("{id}/tecnico-externo")]
    [Authorize(Roles = RolesGestion)]
    public async Task<IActionResult> RegistrarTecnicoExterno(string id, [FromBody] TecnicoExternoRequest request)
    {
        var resultado = await _adminService.RegistrarTecnicoExternoAsync(id, request, User.ObtenerUsuarioActual());
        return Ok(ApiResponse<IncidenciaResponse>.Ok(resultado, "Proveedor externo registrado exitosamente"));
    }

    /// <remarks>
    /// Corrige la captura inicial: reemplaza `titulo`, `descripcion`, `ubicacion` y `detalle_especifico`.
    ///
    /// **Roles:**
    ///
    /// - **Root, Soporte de TI:** cualquier incidencia, en cualquier estado.
    /// - **Reportante:** solo las suyas y **solo mientras están `abierta`**. Si ya pasó a `en proceso`, `resuelta`
    ///   o `cancelada`, se rechaza con error de permisos (403).
    ///
    /// La categoría no se cambia: `detalleEspecifico` se valida contra los campos dinámicos de la categoría actual.
    /// `claveUbicacion` debe existir en el catálogo de edificios.
    /// **Bitácora:** registra qué campos se corrigieron. Si no cambia nada, no se guarda nada.
    /// </remarks>
    /// <param name="id">_id de la incidencia.</param>
    /// <param name="request">Datos generales corregidos.</param>
    /// <response code="200">Incidencia actualizada.</response>
    /// <response code="400">Datos inválidos (lista de errores por campo).</response>
    /// <response code="403">No es su incidencia, o ya no está abierta (Reportante).</response>
    /// <response code="404">La incidencia no existe.</response>
    /// <response code="409">La incidencia cambió al mismo tiempo.</response>
    [HttpPut("{id}")]
    [Authorize(Roles = RolesSistema.Root + "," + RolesSistema.Soporte + "," + RolesSistema.Reportante)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateIncidenciaRequest request)
    {
        var resultado = await _adminService.UpdateAsync(id, request, User.ObtenerUsuarioActual());
        return Ok(ApiResponse<IncidenciaResponse>.Ok(resultado, "Incidencia actualizada exitosamente"));
    }

    /// <remarks>
    /// Elimina la incidencia por completo (borrado físico, no se puede deshacer).
    ///
    /// **Roles:** Root, Administrativos.
    ///
    /// 1. Lee `evidencias_adjuntas` y toma los `public_id`.
    /// 2. Borra cada archivo de Cloudinary (`DestroyAsync`).
    /// 3. Solo si todos se borraron, elimina el documento de MongoDB (`DeleteOneAsync`).
    ///
    /// Si Cloudinary falla, la incidencia **no** se borra (503) y se puede reintentar.
    /// </remarks>
    /// <param name="id">_id de la incidencia.</param>
    /// <response code="200">Incidencia eliminada; incluye cuántos archivos se borraron.</response>
    /// <response code="400">El id no tiene formato válido.</response>
    /// <response code="403">El rol no puede eliminar incidencias.</response>
    /// <response code="404">La incidencia no existe.</response>
    /// <response code="503">No se pudieron borrar las evidencias en Cloudinary.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = RolesSistema.Root + "," + RolesSistema.Administrativos)]
    public async Task<IActionResult> Delete(string id)
    {
        var resultado = await _adminService.DeleteAsync(id);
        return Ok(ApiResponse<IncidenciaEliminadaResponse>.Ok(resultado, "Incidencia eliminada exitosamente"));
    }
}
