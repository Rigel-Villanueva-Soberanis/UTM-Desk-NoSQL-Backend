using Application.Common.Bitacora;
using Application.Common.ErrorCatalog;
using Application.Common.ErrorsIncidencias;
using Application.Common.Seguridad;
using Application.Common.Utilidades;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Application.Validators.Incidencias;
using AutoMapper;
using Domain;

namespace Application.Services;

/// <summary>
/// Casos de uso de la Fase 4: etiquetas, proveedor externo, corrección de captura y borrado físico.
/// Toda modificación se guarda con <see cref="IIncidenciaRepository.AplicarCambiosAsync"/>, que SIEMPRE
/// agrega su entrada de bitácora (armada con <see cref="BitacoraIncidencia"/>) en la misma operación.
/// </summary>
public class AdministracionIncidenciaService
{
    private readonly IIncidenciaRepository _repository;
    private readonly IArchivoRepository _archivoRepository;
    private readonly IncidenciaValidator _validator;
    private readonly IMapper _mapper;

    public AdministracionIncidenciaService(
        IIncidenciaRepository repository,
        IArchivoRepository archivoRepository,
        IncidenciaValidator validator,
        IMapper mapper)
    {
        _repository        = repository;
        _archivoRepository = archivoRepository;
        _validator         = validator;
        _mapper            = mapper;
    }

    // ─── GET /api/incidencias/tags-sugeridos ────────────────────────────────

    /// <summary>
    /// Etiquetas distintas de todas las incidencias (distinct en MongoDB), en orden alfabético.
    /// Con <paramref name="busqueda"/> devuelve solo las que la contienen (para autocompletar).
    /// </summary>
    public async Task<List<string>> GetTagsSugeridosAsync(string? busqueda)
    {
        NoSqlSanitizer.Validar(busqueda, "busqueda");
        var texto = TagValidator.Normalizar(busqueda);

        var tags = await _repository.GetTagsDistintosAsync();

        return tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Where(t => texto.Length == 0 || TagValidator.Normalizar(t).Contains(texto))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    // ─── PATCH /api/incidencias/{id}/tags ───────────────────────────────────

    /// <summary>
    /// Agrega ($addToSet) y/o quita ($pull) etiquetas ya normalizadas. Devuelve las etiquetas finales.
    /// Técnico: solo en las incidencias que tiene asignadas.
    /// Las que ya tenía (al agregar) o no tenía (al quitar) se ignoran; si no hay nada que cambiar no se guarda nada.
    /// </summary>
    public async Task<List<string>> ActualizarTagsAsync(string id, ActualizarTagsRequest request, UsuarioActual usuario)
    {
        var tags       = TagValidator.Validar(request);
        var incidencia = await ObtenerIncidenciaAsync(id);

        if (usuario.EsRol(RolesSistema.Tecnico) && !IncidenciaValidator.EsTecnicoAsignado(incidencia, usuario))
            throw SeguimientoErrors.NoAsignado();

        var actuales = incidencia.Tags ?? [];
        var agregar  = tags.Agregar.Where(t => !actuales.Contains(t)).ToList();
        var quitar   = tags.Quitar.Where(actuales.Contains).ToList();

        if (agregar.Count == 0 && quitar.Count == 0)
            return actuales;

        var total = actuales.Count + agregar.Count - quitar.Count;
        if (total > TagValidator.MaximoPorIncidencia)
            throw TagErrors.LimiteIncidencia(TagValidator.MaximoPorIncidencia, actuales.Count - quitar.Count);

        var actualizada = await GuardarAsync(id, new CambiosIncidencia
        {
            TagsAgregar     = agregar,
            TagsQuitar      = quitar,
            InicializarTags = incidencia.Tags is null,
            Bitacora        = [BitacoraIncidencia.Etiquetas(usuario, agregar, quitar, incidencia.Estado)]
        });

        return actualizada.Tags ?? [];
    }

    // ─── PATCH /api/incidencias/{id}/tecnico-externo ────────────────────────

    /// <summary>
    /// Registra (o actualiza) el proveedor externo en tecnico_externo y lo anota en la bitácora.
    /// No se permite en incidencias resueltas o canceladas.
    /// </summary>
    public async Task<IncidenciaResponse> RegistrarTecnicoExternoAsync(string id, TecnicoExternoRequest request, UsuarioActual usuario)
    {
        var externo    = TecnicoExternoValidator.Validar(request);
        var incidencia = await ObtenerIncidenciaAsync(id);

        if (IncidenciaValidator.EstaCerrada(incidencia.Estado))
            throw TecnicoExternoErrors.IncidenciaCerrada(incidencia.Estado);

        if (TecnicoExternoValidator.EsIgual(incidencia.TecnicoExterno, externo))
            throw TecnicoExternoErrors.SinCambios();

        var actualizacion = incidencia.TecnicoExterno?.RequiereServicioExterno == true;

        var actualizada = await GuardarAsync(id, new CambiosIncidencia
        {
            EstadoEsperado      = incidencia.Estado,
            NuevoTecnicoExterno = externo,
            Bitacora            = [BitacoraIncidencia.ServicioExterno(
                                       usuario, externo.EmpresaProveedor!, externo.OrdenServicioRef!, actualizacion, incidencia.Estado)]
        });

        return _mapper.Map<IncidenciaResponse>(actualizada);
    }

    // ─── PUT /api/incidencias/{id} ──────────────────────────────────────────

    /// <summary>
    /// Corrige la captura inicial (título, descripción, ubicación y detalle específico).
    ///   • Root y Soporte: en cualquier estado.
    ///   • Reportante: solo sus incidencias y solo mientras están "abierta"; si no, error de permisos (403).
    /// Se guarda con EstadoEsperado: si justo la tomaron (pasó a "en proceso") el cambio no se aplica.
    /// </summary>
    public async Task<IncidenciaResponse> UpdateAsync(string id, UpdateIncidenciaRequest request, UsuarioActual usuario)
    {
        var incidencia = await ObtenerIncidenciaAsync(id);

        if (usuario.EsRol(RolesSistema.Reportante))
        {
            if (incidencia.DatosReportante.IdUsuario != usuario.Id)
                throw EdicionErrors.NoEsTuIncidencia();

            if (incidencia.Estado != EstadoIncidenciaValidator.Abierta)
                throw EdicionErrors.EstadoNoPermite(incidencia.Estado);
        }

        var datos = await _validator.ValidarEdicionAsync(request, incidencia.Categoria);

        var nuevos = new DatosGeneralesIncidencia(
            request.Titulo.Trim(), request.Descripcion.Trim(), datos.Ubicacion, datos.Detalle);

        var cambiados = CamposCambiados(incidencia, nuevos);
        if (cambiados.Count == 0)
            return _mapper.Map<IncidenciaResponse>(incidencia);   // nada que guardar

        var actualizada = await GuardarAsync(id, new CambiosIncidencia
        {
            EstadoEsperado       = incidencia.Estado,
            NuevosDatosGenerales = nuevos,
            Bitacora             = [BitacoraIncidencia.Edicion(usuario, cambiados, incidencia.Estado)]
        });

        return _mapper.Map<IncidenciaResponse>(actualizada);
    }

    // ─── DELETE /api/incidencias/{id} ───────────────────────────────────────

    /// <summary>
    /// Borrado físico:
    ///   1. Lee evidencias_adjuntas y toma sus public_id.
    ///   2. Borra cada archivo de Cloudinary (DestroyAsync).
    ///   3. Solo si TODOS se borraron, elimina el documento de MongoDB (DeleteOneAsync).
    /// Si algún archivo falla, la incidencia NO se borra (para no dejar imágenes huérfanas) y se puede reintentar:
    /// los que ya se borraron cuentan como borrados en el siguiente intento.
    /// </summary>
    public async Task<IncidenciaEliminadaResponse> DeleteAsync(string id)
    {
        var incidencia = await ObtenerIncidenciaAsync(id);

        var publicIds = incidencia.EvidenciasAdjuntas
            .Where(e => !string.IsNullOrWhiteSpace(e.PublicId))
            .Select(e => e.PublicId!)
            .Distinct()
            .ToList();

        var resultados = await Task.WhenAll(publicIds.Select(_archivoRepository.EliminarAsync));

        var fallidos = resultados
            .Where(r => !r.Eliminado)
            .Select(r => EdicionErrors.ArchivoNoEliminado(r.PublicId, r.Motivo ?? "error desconocido"))
            .ToList();

        if (fallidos.Count > 0)
            throw EdicionErrors.ArchivosNoEliminados(fallidos);

        if (!await _repository.DeleteAsync(id))
            throw IncidenciaErrors.NotFound();

        return new IncidenciaEliminadaResponse
        {
            Id                 = incidencia.Id,
            Folio              = incidencia.Folio,
            ArchivosEliminados = resultados.Length
        };
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<Incidencia> ObtenerIncidenciaAsync(string id)
    {
        IncidenciaValidator.ValidarId(id);
        return await _repository.GetByIdAsync(id)
            ?? throw IncidenciaErrors.NotFound();
    }

    /// <summary>Nombres de los campos que cambian (para el texto de la bitácora).</summary>
    private static List<string> CamposCambiados(Incidencia actual, DatosGeneralesIncidencia nuevos)
    {
        var campos = new List<string>();
        if (actual.Titulo != nuevos.Titulo)                       campos.Add("título");
        if (actual.Descripcion != nuevos.Descripcion)             campos.Add("descripción");
        if (actual.Ubicacion.Clave != nuevos.Ubicacion.Clave
            || actual.Ubicacion.Edificio != nuevos.Ubicacion.Edificio
            || actual.Ubicacion.Aula != nuevos.Ubicacion.Aula)    campos.Add("ubicación");
        if (!actual.DetalleEspecifico.Equals(nuevos.DetalleEspecifico)) campos.Add("detalle específico");
        return campos;
    }

    /// <summary>Aplica los cambios (con su bitácora). Si otra persona la cambió al mismo tiempo, avisa.</summary>
    private async Task<Incidencia> GuardarAsync(string id, CambiosIncidencia cambios)
        => await _repository.AplicarCambiosAsync(id, cambios)
           ?? throw IncidenciaErrors.CambioSimultaneo();
}
