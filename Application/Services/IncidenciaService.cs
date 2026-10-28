using Application.Common.Bitacora;
using Application.Common.ErrorCatalog;
using Application.Common.ErrorsIncidencias;
using Application.Common.Seguridad;
using Application.Common.Utilidades;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Application.Validators.Evidencias;
using Application.Validators.Incidencias;
using Domain;
using AutoMapper;
using MongoDB.Bson;

namespace Application.Services;

/// <summary>
/// Casos de uso de incidencias. Solo orquesta: valida (IncidenciaValidator), pide el folio (FolioService),
/// llama al repositorio y mapea la respuesta.
/// Fase 1: registro con folio atómico y consultas base.
/// Fase 2: evidencias adjuntas (archivos en Cloudinary).
/// </summary>
public class IncidenciaService
{
    private readonly IIncidenciaRepository _repository;
    private readonly IArchivoRepository _archivoRepository;
    private readonly IncidenciaValidator _validator;
    private readonly FolioService _folioService;
    private readonly IMapper _mapper;

    public IncidenciaService(
        IIncidenciaRepository repository,
        IArchivoRepository archivoRepository,
        IncidenciaValidator validator,
        FolioService folioService,
        IMapper mapper)
    {
        _repository        = repository;
        _archivoRepository = archivoRepository;
        _validator         = validator;
        _folioService      = folioService;
        _mapper            = mapper;
    }

    // ─── POST /api/incidencias ──────────────────────────────────────────────

    /// <summary>
    /// Registra una incidencia (RF-14, RF-15, RF-16):
    /// valida contra los catálogos, genera el folio atómico en Redis, toma los datos del reportante
    /// del token y la inicializa en estado "abierta" con fechas.resolucion = null.
    /// </summary>
    public async Task<IncidenciaResponse> CreateAsync(CreateIncidenciaRequest request, UsuarioActual usuario)
    {
        var datos = await _validator.ValidarCreacionAsync(request);

        var ahora = DateTime.UtcNow;

        var incidencia = new Incidencia
        {
            Id                = ObjectId.GenerateNewId().ToString(),
            Titulo            = TextoSeguro.Limpiar(request.Titulo),
            Descripcion       = TextoSeguro.Limpiar(request.Descripcion),
            Categoria         = datos.Categoria,
            DetalleEspecifico = datos.Detalle,
            Estado            = IncidenciaValidator.EstadoInicial,
            Prioridad         = datos.Prioridad,
            Ubicacion         = datos.Ubicacion,
            DatosReportante = new DatosReportante
            {
                IdUsuario = usuario.Id,
                Nombre    = usuario.Nombre,
                Perfil    = usuario.Clasificacion
            },
            TecnicoAsignado = null,
            TecnicoExterno  = null,
            HorasInvertidas = 0,
            Tags            = [],
            HistorialSeguimiento = [BitacoraIncidencia.Creacion(usuario, IncidenciaValidator.EstadoInicial)],
            EvidenciasAdjuntas = [],
            Fechas = new FechasIncidencia { Creacion = ahora, Resolucion = null }
        };

        var creada = await _folioService.GuardarConFolioAsync(incidencia);
        return _mapper.Map<IncidenciaResponse>(creada);
    }

    // ─── GET /api/incidencias ───────────────────────────────────────────────

    /// <summary>
    /// Listado paginado con filtros.
    ///   • Reportante → SOLO sus incidencias (RF-04), sin importar lo que mande el cliente.
    ///   • Técnico    → SOLO las incidencias que tiene asignadas.
    ///   • Demás roles → lista global.
    /// </summary>
    public async Task<PaginadoResponse<IncidenciaResumenResponse>> GetAllAsync(IncidenciaFiltroRequest filtro, UsuarioActual usuario)
    {
        var filtros = IncidenciaValidator.ValidarFiltros(filtro);

        var consulta = new IncidenciaConsulta(
            Pagina:       filtro.Pagina,
            TamanoPagina: filtro.TamanoPagina,
            IdReportante: usuario.EsRol(RolesSistema.Reportante) ? usuario.Id : null,   // RF-04
            IdTecnico:    usuario.EsRol(RolesSistema.Tecnico)    ? usuario.Id : null,
            Estado:       filtros.Estado,
            Prioridad:    filtros.Prioridad,
            Categoria:    filtros.Categoria,
            Edificio:     filtros.Edificio,
            Asignada:     filtro.Asignada,
            Tag:          filtros.Tag,
            ConProveedorExterno: filtro.ConProveedorExterno,
            Desde:        filtros.Desde,
            Hasta:        filtros.Hasta);

        var (items, total) = await _repository.GetPaginadoAsync(consulta);

        return new PaginadoResponse<IncidenciaResumenResponse>
        {
            Items        = _mapper.Map<List<IncidenciaResumenResponse>>(items),
            Pagina       = consulta.Pagina,
            TamanoPagina = consulta.TamanoPagina,
            Total        = total,
            TotalPaginas = (int)Math.Ceiling(total / (double)consulta.TamanoPagina)
        };
    }

    // ─── GET /api/incidencias/{id} ──────────────────────────────────────────

    /// <summary>
    /// Expediente completo en una sola lectura por _id (RNF-01, RNF-02).
    ///   • Reportante → solo si es el creador.
    ///   • Técnico    → solo si es el técnico asignado.
    ///   • Demás roles → cualquier incidencia.
    /// </summary>
    public async Task<IncidenciaResponse> GetByIdAsync(string id, UsuarioActual usuario)
    {
        IncidenciaValidator.ValidarId(id);

        var incidencia = await _repository.GetByIdAsync(id)
            ?? throw IncidenciaErrors.NotFound();

        if (!IncidenciaValidator.PuedeAcceder(incidencia, usuario))
            throw IncidenciaErrors.SinAcceso();

        return _mapper.Map<IncidenciaResponse>(incidencia);
    }

    // ─── PATCH /api/incidencias/{id}/evidencias ─────────────────────────────

    /// <summary>
    /// Liga a la incidencia de 1 a 3 archivos ya subidos a Cloudinary con $push + $each sobre evidencias_adjuntas (RF-19).
    /// Inyecta los datos del usuario (del token) y la fecha actual.
    ///   • Reportante → solo en sus propias incidencias (evidencia inicial).
    ///   • Técnico    → solo en las incidencias que tiene asignadas (evidencia de reparación).
    ///   • Root       → en cualquiera.
    /// Los archivos deben estar en la carpeta del propio usuario y no estar ya ligados.
    /// Límite por rol en cada incidencia: Reportante 3 evidencias suyas, Técnico 3 suyas (Root sin límite).
    /// </summary>
    public async Task<List<EvidenciaResponse>> AgregarEvidenciasAsync(string id, AgregarEvidenciasRequest request, UsuarioActual usuario)
    {
        IncidenciaValidator.ValidarId(id);

        var incidencia = await _repository.GetByIdAsync(id)
            ?? throw IncidenciaErrors.NotFound();

        if (!IncidenciaValidator.PuedeAcceder(incidencia, usuario))
            throw EvidenciaErrors.SinPermiso();

        if (IncidenciaValidator.EstaFinalizada(incidencia.Estado))
            throw EvidenciaErrors.EstadoNoPermite(incidencia.Estado);

        var yaLigadas = incidencia.EvidenciasAdjuntas
            .Where(e => e.PublicId is not null)
            .Select(e => e.PublicId!)
            .ToList();

        var errores    = new List<ErrorDetalle>();
        var validadas  = EvidenciaValidator.ValidarReferencias(request.Evidencias, usuario.Id, errores, yaLigadas);

        if (errores.Count > 0)
            throw EvidenciaErrors.DatosInvalidos(errores);

        // Límite por rol: cuenta solo las evidencias que ya subió alguien de ese mismo rol
        var limite = EvidenciaValidator.LimiteDeRol(usuario.Rol);
        if (limite is not null)
        {
            var actuales = incidencia.EvidenciasAdjuntas.Count(e => TextoNormalizer.Iguales(e.SubidoPor?.Rol, usuario.Rol));
            if (actuales + validadas.Count > limite.Value)
                throw EvidenciaErrors.LimitePorRol(usuario.Rol, limite.Value, actuales);
        }

        // Peso real de cada archivo (y que exista de verdad en el almacenamiento)
        var consultas = await Task.WhenAll(validadas.Select(v => _archivoRepository.ConsultarAsync(v.PublicId)));
        if (consultas.Any(c => c.Estado == EstadoConsulta.NoDisponible))
            throw EvidenciaErrors.AlmacenamientoNoDisponible();

        var inexistentes = consultas
            .Select((c, i) => (c, i))
            .Where(x => x.c.Estado == EstadoConsulta.NoExiste)
            .Select(x => EvidenciaErrors.ArchivoNoExiste($"evidencias[{x.i}].publicId"))
            .ToList();
        if (inexistentes.Count > 0)
            throw EvidenciaErrors.DatosInvalidos(inexistentes);

        var pesos = validadas.Select((v, i) => (v.PublicId, consultas[i].Bytes)).ToDictionary(x => x.PublicId, x => x.Bytes);

        var (evidencias, bitacora) = CrearEvidencias(validadas, pesos, usuario, incidencia.Estado);

        _ = await _repository.AplicarCambiosAsync(id, new CambiosIncidencia
        {
            EstadoEsperado   = incidencia.Estado,
            EvidenciasNuevas = evidencias,
            Bitacora         = bitacora
        }) ?? throw IncidenciaErrors.CambioSimultaneo();

        return _mapper.Map<List<EvidenciaResponse>>(evidencias);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Convierte las evidencias validadas en subdocumentos (firmados por el usuario y con fecha)
    /// y arma una entrada de bitácora por cada una.
    /// </summary>
    private (List<EvidenciaIncidencia> Evidencias, List<SeguimientoIncidencia> Bitacora) CrearEvidencias(
        IEnumerable<EvidenciaValidada> validadas, IReadOnlyDictionary<string, long> pesos, UsuarioActual usuario, string estado)
    {
        var evidencias = new List<EvidenciaIncidencia>();
        var bitacora   = new List<SeguimientoIncidencia>();

        foreach (var validada in validadas)
        {
            evidencias.Add(new EvidenciaIncidencia
            {
                NombreArchivo = validada.NombreArchivo,
                UrlStorage    = _archivoRepository.ConstruirUrl(validada.PublicId, validada.Formato),
                PublicId      = validada.PublicId,
                Formato       = validada.Formato,
                TipoMime      = EvidenciaValidator.TipoMimeDe(validada.Formato),
                PesoKb        = EvidenciaValidator.PesoEnKb(pesos[validada.PublicId]),
                SubidoPor     = BitacoraIncidencia.Autor(usuario),
                FechaCarga    = DateTime.UtcNow
            });

            bitacora.Add(BitacoraIncidencia.Evidencia(usuario, validada.NombreArchivo, estado));
        }

        return (evidencias, bitacora);
    }
}
