using Api.Common;
using Api.Security;
using Application.Common.Seguridad;
using Application.DTOs.Responses;
using Application.DTOs.Requests;
using Application.Services;
using Application.Validators.Evidencias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Evidencias — Fase 2: subida de archivos a Cloudinary.
/// </summary>
[ApiController]
[Route("api/evidencias")]
[Produces("application/json")]
[Authorize]
public class EvidenciaController : ControllerBase
{
    private readonly EvidenciaService _service;

    public EvidenciaController(EvidenciaService service)
    {
        _service = service;
    }

    /// <remarks>
    /// Sube de 1 a 3 archivos de evidencia a Cloudinary en una sola petición.
    ///
    /// **Roles:** Root, Reportante, Técnico.
    ///
    /// - Formatos: jpg, jpeg, png, webp, pdf. Máximo 10 MB por archivo.
    /// - Se validan **todos** antes de subir: si uno está mal, no se sube ninguno (errores en `archivos[i]`).
    /// - Las **imágenes** se convierten a **WebP**, se comprimen y se limitan a 1920 px por lado antes de guardarse.
    ///   Los PDF se guardan tal cual.
    /// - Los archivos quedan con **acceso restringido**: no tienen URL pública. La respuesta trae, de cada uno, `publicId`, `formato`,
    ///   `nombreArchivo`, `bytes` y un enlace firmado `urlTemporal` (para previsualizar) que **vence** a los 15 minutos (`venceEn`, UTC).
    /// - Después se ligan a la incidencia con `PATCH /api/incidencias/{id}/evidencias`.
    /// </remarks>
    /// <param name="archivos">Archivos a subir (campo "archivos" del form-data, se puede repetir hasta 3 veces).</param>
    /// <response code="201">Archivos subidos.</response>
    /// <response code="400">Sin archivos, más de 3, o alguno vacío, muy grande o con formato no permitido.</response>
    /// <response code="401">Sin token o token inválido.</response>
    /// <response code="403">El rol no puede subir evidencias.</response>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(EvidenciaValidator.TamanoMaximoBytes * EvidenciaValidator.MaxArchivosPorPeticion + 1024 * 1024)]
    [Authorize(Roles = RolesSistema.Root + "," + RolesSistema.Reportante + "," + RolesSistema.TecnicoToken + "," + RolesSistema.Tecnico)]
    public async Task<IActionResult> Upload(List<IFormFile>? archivos)
    {
        // IFormFile se queda en el controller; a Application solo llega el contenido
        var entradas = (archivos ?? [])
            .Select(a => new ArchivoEntrada(a.OpenReadStream(), a.FileName, a.ContentType, a.Length))
            .ToList();

        try
        {
            var subidos = await _service.SubirAsync(entradas, User.ObtenerUsuarioActual());
            return StatusCode(201, ApiResponse<List<EvidenciaSubidaResponse>>.Ok(subidos, "Archivos subidos exitosamente"));
        }
        finally
        {
            foreach (var entrada in entradas)
                await entrada.Contenido.DisposeAsync();
        }
    }

    /// <summary>
    /// Purga archivos de Cloudinary. Lo invoca el Trigger de Atlas al borrar una incidencia (TTL 60 días).
    /// Autenticación por header X-Api-Key.
    /// </summary>
    [HttpPost("purgar")]
    [AllowAnonymous]
    [ApiKeyAuth]
    public async Task<IActionResult> Purgar([FromBody] PurgarArchivosRequest request)
    {
        var resultado = await _service.PurgarAsync(request);
        return Ok(ApiResponse<PurgarArchivosResponse>.Ok(resultado, "Archivos purgados"));
    }
}
