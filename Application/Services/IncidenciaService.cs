using Application.Common;
using Application.Common.ErrorCatalog;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using Application.Validators.Incidencias;
using AutoMapper;
using Domain;
using MongoDB.Bson;
using System.Text.Json;

namespace Application.Services;

/// <summary>
/// Casos de uso de incidencias — Fase 1: registro con folio atómico y consultas base.
/// </summary>
public class IncidenciaService
{
    private const string CatalogoCategorias = "categorias_incidencia";
    private const string CatalogoEdificios  = "edificios";
    private const string EstadoInicial      = "abierta";
    private const string PrioridadDefault   = "media";
    private const int    MaxIntentosFolio   = 3;

    private static readonly string[] Estados     = ["abierta", "en proceso", "por autorizar", "resuelta", "cancelada"];
    private static readonly string[] Prioridades = ["baja", "media", "alta", "critica"];

    private readonly IIncidenciaRepository _repository;
    private readonly ICatalogoRepository _catalogoRepository;
    private readonly IFolioRepository _folioRepository;
    private readonly IMapper _mapper;

    public IncidenciaService(
        IIncidenciaRepository repository,
        ICatalogoRepository catalogoRepository,
        IFolioRepository folioRepository,
        IMapper mapper)
    {
        _repository         = repository;
        _catalogoRepository = catalogoRepository;
        _folioRepository    = folioRepository;
        _mapper             = mapper;
    }

    // ─── POST /api/incidencias ──────────────────────────────────────────────

    /// <summary>
    /// Registra una incidencia (RF-14, RF-15, RF-16):
    /// valida contra los catálogos, genera el folio atómico en Redis, toma los datos del reportante
    /// del token y la inicializa en estado "abierta" con fechas.resolucion = null.
    /// </summary>
    public async Task<IncidenciaResponse> CreateAsync(CreateIncidenciaRequest request, UsuarioActual usuario)
    {
        NoSqlSanitizer.ValidarTodos(
            (request.Titulo,         "titulo"),
            (request.Categoria,      "categoria"),
            (request.ClaveUbicacion, "claveUbicacion"),
            (request.Prioridad,      "prioridad")
        );

        var errores = new List<ErrorDetalle>();

        // Prioridad (opcional, por defecto "media")
        var prioridad = string.IsNullOrWhiteSpace(request.Prioridad)
            ? PrioridadDefault
            : TextoNormalizer.SinAcentos(request.Prioridad);
        if (!Prioridades.Contains(prioridad))
            errores.Add(new ErrorDetalle("prioridad", $"'{request.Prioridad}' no es válida. Opciones: {string.Join(", ", Prioridades)}."));

        // Categoría + detalle específico (contra categorias_incidencia)
        var categorias = await ObtenerCatalogoAsync(CatalogoCategorias);
        var categoria  = BuscarElemento(categorias, request.Categoria);
        var detalle    = new BsonDocument();

        if (categoria is null)
        {
            errores.Add(new ErrorDetalle("categoria", $"La categoría '{request.Categoria}' no existe. Opciones: {string.Join(", ", Claves(categorias))}."));
        }
        else
        {
            var campos = categoria.Contains("campos_dinamicos") && categoria["campos_dinamicos"].IsBsonArray
                ? categoria["campos_dinamicos"].AsBsonArray
                : new BsonArray();
            detalle = DetalleEspecificoValidator.Validar(JsonToBsonValue(request.DetalleEspecifico), campos, errores);
        }

        // Ubicación (contra edificios)
        var edificios = await ObtenerCatalogoAsync(CatalogoEdificios);
        var lugar     = BuscarElemento(edificios, request.ClaveUbicacion);
        if (lugar is null)
            errores.Add(new ErrorDetalle("claveUbicacion", $"La ubicación '{request.ClaveUbicacion}' no existe en el catálogo de edificios."));

        if (errores.Count > 0)
            throw IncidenciaErrors.DatosInvalidos(errores);

        var ahora = DateTime.UtcNow;

        var autor = new UsuarioReferencia { IdUsuario = usuario.Id, Nombre = usuario.Nombre, Rol = usuario.Rol };

        var incidencia = new Incidencia
        {
            Id                = ObjectId.GenerateNewId().ToString(),
            Titulo            = request.Titulo.Trim(),
            Descripcion       = request.Descripcion.Trim(),
            Categoria         = categoria!["clave"].AsString,
            DetalleEspecifico = detalle,
            Estado            = EstadoInicial,
            Prioridad         = prioridad,
            Ubicacion = new UbicacionIncidencia
            {
                Clave    = lugar!["clave"].AsString,
                Edificio = lugar["edificio"].AsString,
                Aula     = lugar.Contains("salon") && lugar["salon"].IsString ? lugar["salon"].AsString : null
            },
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
            HistorialSeguimiento =
            [
                new SeguimientoIncidencia
                {
                    Autor           = autor,
                    Accion          = "Incidencia registrada en el sistema.",
                    EstadoReportado = EstadoInicial,
                    Fecha           = ahora
                }
            ],
            EvidenciasAdjuntas = [],
            Fechas = new FechasIncidencia { Creacion = ahora, Resolucion = null }
        };

        var creada = await GuardarConFolioAsync(incidencia, AnioLocal(ahora));
        return _mapper.Map<IncidenciaResponse>(creada);
    }

    // ─── GET /api/incidencias ───────────────────────────────────────────────

    /// <summary>
    /// Listado paginado con filtros.
    ///   • Reportante → SOLO sus incidencias (RF-04), sin importar lo que mande el cliente.
    ///   • Técnico    → cola global de trabajo (RF-05).
    ///   • Demás roles → lista global.
    /// </summary>
    public async Task<PaginadoResponse<IncidenciaResumenResponse>> GetAllAsync(IncidenciaFiltroRequest filtro, UsuarioActual usuario)
    {
        NoSqlSanitizer.ValidarTodos(
            (filtro.Estado,    "estado"),
            (filtro.Prioridad, "prioridad"),
            (filtro.Categoria, "categoria"),
            (filtro.Edificio,  "edificio")
        );

        var errores = new List<ErrorDetalle>();

        var estado = ValorDeLista(filtro.Estado, Estados);
        if (!string.IsNullOrWhiteSpace(filtro.Estado) && estado is null)
            errores.Add(new ErrorDetalle("estado", $"'{filtro.Estado}' no es válido. Opciones: {string.Join(", ", Estados)}."));

        var prioridad = ValorDeLista(filtro.Prioridad, Prioridades);
        if (!string.IsNullOrWhiteSpace(filtro.Prioridad) && prioridad is null)
            errores.Add(new ErrorDetalle("prioridad", $"'{filtro.Prioridad}' no es válida. Opciones: {string.Join(", ", Prioridades)}."));

        if (errores.Count > 0)
            throw IncidenciaErrors.FiltrosInvalidos(errores);

        var consulta = new IncidenciaConsulta(
            Pagina:       filtro.Pagina,
            TamanoPagina: filtro.TamanoPagina,
            IdReportante: usuario.EsRol(RolesSistema.Reportante) ? usuario.Id : null,   // RF-04
            Estado:       estado,
            Prioridad:    prioridad,
            Categoria:    string.IsNullOrWhiteSpace(filtro.Categoria) ? null : TextoNormalizer.NormalizarClave(filtro.Categoria),
            Edificio:     string.IsNullOrWhiteSpace(filtro.Edificio) ? null : filtro.Edificio.Trim());

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
    ///   • Técnico y demás roles → cualquier incidencia (el técnico la revisa desde la cola global).
    /// </summary>
    public async Task<IncidenciaResponse> GetByIdAsync(string id, UsuarioActual usuario)
    {
        if (!ObjectId.TryParse(id, out _))
            throw IncidenciaErrors.IdInvalido();

        var incidencia = await _repository.GetByIdAsync(id)
            ?? throw IncidenciaErrors.NotFound();

        if (usuario.EsRol(RolesSistema.Reportante) && incidencia.DatosReportante.IdUsuario != usuario.Id)
            throw IncidenciaErrors.SinAcceso();

        return _mapper.Map<IncidenciaResponse>(incidencia);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    // ─── Folio ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Asigna el folio y guarda. El folio se pide hasta que todo es válido (para no "gastar" números).
    /// Si Mongo rechaza el folio por repetido (Redis perdió su contador), se resincroniza y se reintenta.
    /// </summary>
    private async Task<Incidencia> GuardarConFolioAsync(Incidencia incidencia, int anio)
    {
        for (var intento = 1; intento <= MaxIntentosFolio; intento++)
        {
            incidencia.Folio = await GenerarFolioAsync(anio);
            try
            {
                return await _repository.CreateAsync(incidencia);
            }
            catch (FolioDuplicadoException)
            {
                await SincronizarContadorAsync(anio);
            }
        }
        throw IncidenciaErrors.FolioNoAsignado();
    }

    /// <summary>
    /// Folio INC-AAAA-XXXX con Redis INCR (una llave por año, así se reinicia cada año).
    /// Si el contador sale en 1 puede ser un año nuevo o que Redis perdió sus datos:
    /// se revisa en Mongo el último folio del año y, si existe, se continúa desde ahí.
    /// </summary>
    private async Task<string> GenerarFolioAsync(int anio)
    {
        var clave       = ClaveContador(anio);
        var consecutivo = await _folioRepository.IncrementarAsync(clave);

        if (consecutivo == 1 && await SincronizarContadorAsync(anio))
            consecutivo = await _folioRepository.IncrementarAsync(clave);

        return $"{PrefijoFolio(anio)}{consecutivo:D4}";
    }

    /// <summary>
    /// Pone el contador de Redis al nivel del último folio guardado en Mongo para ese año.
    /// Devuelve true si encontró folios existentes.
    /// </summary>
    private async Task<bool> SincronizarContadorAsync(int anio)
    {
        var prefijo = PrefijoFolio(anio);
        var ultimo  = await _repository.ObtenerUltimoFolioAsync(prefijo);

        if (ultimo is null || !long.TryParse(ultimo[prefijo.Length..], out var numero))
            return false;

        await _folioRepository.AsegurarMinimoAsync(ClaveContador(anio), numero);
        return true;
    }

    private static string PrefijoFolio(int anio)  => $"INC-{anio}-";
    private static string ClaveContador(int anio) => $"utmdesk:folio:incidencias:{anio}";

    /// <summary>Año según la hora de México (para que el 31 de diciembre en la noche no salte de año).</summary>
    private static int AnioLocal(DateTime ahoraUtc)
    {
        try
        {
            var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");
            return TimeZoneInfo.ConvertTimeFromUtc(ahoraUtc, zona).Year;
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return ahoraUtc.Year;
        }
    }

    private async Task<Catalogo> ObtenerCatalogoAsync(string clave)
        => await _catalogoRepository.GetByClaveAsync(clave)
           ?? throw IncidenciaErrors.CatalogoNoDisponible(clave);

    /// <summary>Busca un elemento del catálogo por su "clave" (ignora mayúsculas, espacios y acentos).</summary>
    private static BsonDocument? BuscarElemento(Catalogo catalogo, string? clave)
    {
        if (!catalogo.Contenido.IsBsonArray || string.IsNullOrWhiteSpace(clave)) return null;

        var buscada = TextoNormalizer.NormalizarClave(clave);
        return catalogo.Contenido.AsBsonArray
            .OfType<BsonDocument>()
            .FirstOrDefault(d => d.Contains("clave") && d["clave"].IsString
                                 && TextoNormalizer.NormalizarClave(d["clave"].AsString) == buscada);
    }

    private static IEnumerable<string> Claves(Catalogo catalogo)
        => catalogo.Contenido.IsBsonArray
            ? catalogo.Contenido.AsBsonArray.OfType<BsonDocument>()
                .Where(d => d.Contains("clave") && d["clave"].IsString)
                .Select(d => d["clave"].AsString)
            : [];

    /// <summary>Devuelve el valor oficial de la lista que coincide (ignora mayúsculas y acentos), o null.</summary>
    private static string? ValorDeLista(string? valor, string[] lista)
        => string.IsNullOrWhiteSpace(valor) ? null : lista.FirstOrDefault(v => TextoNormalizer.Iguales(v, valor));

    private static BsonValue JsonToBsonValue(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return BsonNull.Value;

        try
        {
            return BsonDocument.Parse($"{{\"v\":{element.GetRawText()}}}")["v"];
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            throw IncidenciaErrors.DatosInvalidos([new ErrorDetalle("detalleEspecifico", "Tiene un formato inválido.")]);
        }
    }
}
