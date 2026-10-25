using Application.Common.ErrorCatalog;
using Application.Common.ErrorsIncidencias;
using Application.Common.Seguridad;
using Application.Common.Utilidades;
using Application.DTOs.Requests;
using Application.Interfaces;
using Domain;
using MongoDB.Bson;
using System.Text.Json;

namespace Application.Validators.Incidencias;

/// <summary>Datos de una incidencia nueva ya validados contra los catálogos y normalizados.</summary>
public record IncidenciaValidada(string Prioridad, string Categoria, BsonDocument Detalle, UbicacionIncidencia Ubicacion);

/// <summary>Datos generales de una edición ya validados (detalle contra la categoría actual y ubicación del catálogo).</summary>
public record EdicionValidada(BsonDocument Detalle, UbicacionIncidencia Ubicacion);

/// <summary>Filtros del listado ya validados y normalizados.</summary>
/// <summary>
/// Filtros del listado ya validados y normalizados.
/// <see cref="Desde"/> y <see cref="Hasta"/> ya están en UTC; <see cref="Hasta"/> es exclusivo (inicio del día siguiente).
/// </summary>
public record FiltrosValidados(
    string? Estado, string? Prioridad, string? Categoria, string? Edificio,
    string? Tag, DateTime? Desde, DateTime? Hasta);

/// <summary>
/// Reglas de validación de incidencias:
///   • prioridad y estado contra sus listas de valores;
///   • categoría, detalle específico y ubicación contra los catálogos.
/// Los mensajes de error están en <see cref="IncidenciaErrors"/>.
/// </summary>
public class IncidenciaValidator
{
    private const string CatalogoCategorias = "categorias_incidencia";
    private const string CatalogoEdificios  = "edificios";

    public const string EstadoInicial    = "abierta";
    public const string PrioridadDefault = "media";

    /// <summary>Mismos valores que el enum de la validación de la colección en MongoDB.</summary>
    public static readonly string[] Estados     = ["abierta", "en proceso", "por autorizar", "resuelta", "cancelada"];
    public static readonly string[] Prioridades = ["baja", "media", "alta", "critica"];

    private static readonly string[] EstadosCerrados = ["resuelta", "cancelada"];

    private readonly ICatalogoRepository _catalogoRepository;

    public IncidenciaValidator(ICatalogoRepository catalogoRepository)
    {
        _catalogoRepository = catalogoRepository;
    }

    // ─── Crear incidencia ────────────────────────────────────────────────────

    /// <summary>
    /// Valida una incidencia nueva. Junta todos los errores y, si hay alguno,
    /// lanza <see cref="IncidenciaErrors.DatosInvalidos"/> con la lista.
    /// </summary>
    public async Task<IncidenciaValidada> ValidarCreacionAsync(CreateIncidenciaRequest request)
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
            errores.Add(IncidenciaErrors.PrioridadNoValida(request.Prioridad, Prioridades));

        // Categoría + detalle específico (contra categorias_incidencia)
        var categorias = await ObtenerCatalogoAsync(CatalogoCategorias);
        var categoria  = BuscarElemento(categorias, request.Categoria);
        var detalle    = new BsonDocument();

        if (categoria is null)
        {
            errores.Add(IncidenciaErrors.CategoriaNoExiste(request.Categoria, Claves(categorias)));
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
            errores.Add(IncidenciaErrors.UbicacionNoExiste(request.ClaveUbicacion));
        if (errores.Count > 0)
            throw IncidenciaErrors.DatosInvalidos(errores);

        return new IncidenciaValidada(prioridad, categoria!["clave"].AsString, detalle, CrearUbicacion(lugar!));
    }

    // ─── Editar incidencia (corrección de captura) ───────────────────────────

    /// <summary>
    /// Valida la edición de los datos generales. El detalle específico se revisa contra los
    /// campos dinámicos de la categoría que YA tiene la incidencia (la categoría no se edita).
    /// </summary>
    public async Task<EdicionValidada> ValidarEdicionAsync(UpdateIncidenciaRequest request, string categoriaActual)
    {
        NoSqlSanitizer.ValidarTodos(
            (request.Titulo,         "titulo"),
            (request.ClaveUbicacion, "claveUbicacion")
        );

        var errores = new List<ErrorDetalle>();

        var categorias = await ObtenerCatalogoAsync(CatalogoCategorias);
        var categoria  = BuscarElemento(categorias, categoriaActual);
        var detalle    = new BsonDocument();

        if (categoria is null)
        {
            errores.Add(IncidenciaErrors.CategoriaNoExiste(categoriaActual, Claves(categorias)));
        }
        else
        {
            var campos = categoria.Contains("campos_dinamicos") && categoria["campos_dinamicos"].IsBsonArray
                ? categoria["campos_dinamicos"].AsBsonArray
                : new BsonArray();
            detalle = DetalleEspecificoValidator.Validar(JsonToBsonValue(request.DetalleEspecifico), campos, errores);
        }

        var edificios = await ObtenerCatalogoAsync(CatalogoEdificios);
        var lugar     = BuscarElemento(edificios, request.ClaveUbicacion);
        if (lugar is null)
            errores.Add(IncidenciaErrors.UbicacionNoExiste(request.ClaveUbicacion));

        if (errores.Count > 0)
            throw IncidenciaErrors.DatosInvalidos(errores);

        return new EdicionValidada(detalle, CrearUbicacion(lugar!));
    }

    // ─── Filtros del listado ─────────────────────────────────────────────────

    /// <summary>Valida y normaliza los filtros del listado; lanza <see cref="IncidenciaErrors.FiltrosInvalidos"/> si hay errores.</summary>
    public static FiltrosValidados ValidarFiltros(IncidenciaFiltroRequest filtro)
    {
        var errores = new List<ErrorDetalle>();
        var filtros = ValidarFiltros(filtro, errores);

        if (errores.Count > 0)
            throw IncidenciaErrors.FiltrosInvalidos(errores);

        return filtros;
    }

    /// <summary>
    /// Búsqueda full-text: valida el término (mínimo <see cref="MinimoBusqueda"/> caracteres) y los mismos filtros del listado.
    /// </summary>
    public static (FiltrosValidados Filtros, string Texto) ValidarBusqueda(BusquedaIncidenciaRequest request)
    {
        NoSqlSanitizer.Validar(request.Q, "q");

        var errores = new List<ErrorDetalle>();
        var texto   = (request.Q ?? string.Empty).Trim();
        if (texto.Length < MinimoBusqueda)
            errores.Add(IncidenciaErrors.BusquedaMuyCorta(MinimoBusqueda));

        var filtros = ValidarFiltros(request, errores);

        if (errores.Count > 0)
            throw IncidenciaErrors.FiltrosInvalidos(errores);

        return (filtros, texto);
    }

    public const int MinimoBusqueda = 2;

    /// <summary>Valida los filtros juntando los errores en <paramref name="errores"/>.</summary>
    private static FiltrosValidados ValidarFiltros(IncidenciaFiltroRequest filtro, List<ErrorDetalle> errores)
    {
        NoSqlSanitizer.ValidarTodos(
            (filtro.Estado,    "estado"),
            (filtro.Prioridad, "prioridad"),
            (filtro.Categoria, "categoria"),
            (filtro.Edificio,  "edificio"),
            (filtro.Tag,       "tag")
        );

        var estado = ValorDeLista(filtro.Estado, Estados);
        if (!string.IsNullOrWhiteSpace(filtro.Estado) && estado is null)
            errores.Add(IncidenciaErrors.EstadoNoValido(filtro.Estado, Estados));

        var prioridad = PrioridadDeFiltro(filtro.Prioridad, errores);
        var (desde, hasta) = RangoFechas(filtro.FechaDesde, filtro.FechaHasta, errores);

        return new FiltrosValidados(
            estado,
            prioridad,
            CategoriaDeFiltro(filtro.Categoria),
            string.IsNullOrWhiteSpace(filtro.Edificio) ? null : filtro.Edificio.Trim(),
            string.IsNullOrWhiteSpace(filtro.Tag)      ? null : TagValidator.Normalizar(filtro.Tag),
            desde,
            hasta);
    }

    // ─── Piezas reutilizables para filtros (también las usa la analítica) ───

    /// <summary>Prioridad oficial ("Crítica" → "critica") o null; agrega el error si no existe.</summary>
    public static string? PrioridadDeFiltro(string? valor, List<ErrorDetalle> errores)
    {
        var prioridad = ValorDeLista(valor, Prioridades);
        if (!string.IsNullOrWhiteSpace(valor) && prioridad is null)
            errores.Add(IncidenciaErrors.PrioridadNoValida(valor, Prioridades));
        return prioridad;
    }

    /// <summary>Clave de categoría normalizada ("Cómputo" → "computo") o null.</summary>
    public static string? CategoriaDeFiltro(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : TextoNormalizer.NormalizarClave(valor);

    /// <summary>
    /// Convierte días de México a un rango UTC [desde, hasta). Ambos días se incluyen completos.
    /// Agrega el error si la fecha inicial es posterior a la final.
    /// </summary>
    public static (DateTime? Desde, DateTime? Hasta) RangoFechas(DateOnly? desde, DateOnly? hasta, List<ErrorDetalle> errores)
    {
        if (desde is not null && hasta is not null && desde > hasta)
        {
            errores.Add(IncidenciaErrors.RangoFechasInvalido());
            return (null, null);
        }

        return (
            desde is null ? null : HoraMexico.InicioDelDiaUtc(desde.Value),
            hasta is null ? null : HoraMexico.InicioDelDiaUtc(hasta.Value.AddDays(1)));
    }

    // ─── Reglas sueltas ──────────────────────────────────────────────────────

    /// <summary>Lanza <see cref="IncidenciaErrors.IdInvalido"/> si el id no es un ObjectId.</summary>
    public static void ValidarId(string id)
    {
        if (!ObjectId.TryParse(id, out _))
            throw IncidenciaErrors.IdInvalido();
    }

    /// <summary>¿La incidencia ya está cerrada (resuelta o cancelada)?</summary>
    public static bool EstaCerrada(string estado) => EstadosCerrados.Contains(estado);

    /// <summary>¿Puede el usuario ver/trabajar esta incidencia? Reportante: solo las suyas · Técnico: solo las asignadas · Demás roles: todas.</summary>
    public static bool PuedeAcceder(Incidencia incidencia, UsuarioActual usuario)
    {
        if (usuario.EsRol(RolesSistema.Reportante))
            return incidencia.DatosReportante.IdUsuario == usuario.Id;

        if (usuario.EsRol(RolesSistema.Tecnico))
            return EsTecnicoAsignado(incidencia, usuario);

        return true;
    }

    /// <summary>¿El usuario es el técnico asignado a la incidencia?</summary>
    public static bool EsTecnicoAsignado(Incidencia incidencia, UsuarioActual usuario)
        => incidencia.TecnicoAsignado?.IdUsuario == usuario.Id;

    // ─── Helpers ─────────────────────────────────────────────────────────────

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

    /// <summary>Subdocumento ubicacion a partir del elemento del catálogo edificios.</summary>
    private static UbicacionIncidencia CrearUbicacion(BsonDocument lugar) => new()
    {
        Clave    = lugar["clave"].AsString,
        Edificio = lugar["edificio"].AsString,
        Aula     = lugar.Contains("salon") && lugar["salon"].IsString ? lugar["salon"].AsString : null
    };

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
            throw IncidenciaErrors.DatosInvalidos([DetalleEspecificoErrors.FormatoInvalido("detalleEspecifico")]);
        }
    }
}
