using Application.Common;
using Application.Common.ErrorCatalog;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;
using Domain;
using MongoDB.Bson;
using System.Text.Json;

namespace Application.Services;

/// <summary>
/// Casos de uso para la colección de catálogos.
/// Un solo endpoint para todos los catálogos; las reglas de permisos, identificador y contenido
/// de cada uno viven en su <see cref="ICatalogoValidator"/>.
/// </summary>
public class CatalogoService
{
    /// <summary>Campo identificador cuando un catálogo no tiene validador propio.</summary>
    private const string IdentificadorPorDefecto = "clave";

    private readonly ICatalogoRepository _repository;
    private readonly IMapper _mapper;
    private readonly Dictionary<string, ICatalogoValidator> _validadores;

    public CatalogoService(ICatalogoRepository repository, IMapper mapper, IEnumerable<ICatalogoValidator> validadores)
    {
        _repository  = repository;
        _mapper      = mapper;
        _validadores = validadores.ToDictionary(v => v.ClaveCatalogo);
    }

    // ─── Catálogo completo ──────────────────────────────────────────────────

    /// <summary>Lectura de todos los catálogos. Disponible para cualquier rol autenticado.</summary>
    public async Task<IEnumerable<CatalogoResponse>> GetAllAsync()
    {
        var catalogos = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<CatalogoResponse>>(catalogos);
    }

    /// <summary>Lectura de un catálogo por clave. Disponible para cualquier rol autenticado.</summary>
    public async Task<CatalogoResponse> GetByClaveAsync(string claveCatalogo)
    {
        var catalogo = await ObtenerCatalogoAsync(claveCatalogo);
        return _mapper.Map<CatalogoResponse>(catalogo);
    }

    /// <summary>
    /// Reemplaza TODO el contenido de un catálogo.
    /// 1) El catálogo debe tener validador (si no, no es editable).
    /// 2) El rol del usuario debe estar entre los editores del catálogo.
    /// 3) El contenido debe ser array u object (regla de la BD) y cumplir la estructura del catálogo.
    /// </summary>
    public async Task<CatalogoResponse> UpdateAsync(string claveCatalogo, UpdateCatalogoRequest request, string rolUsuario)
    {
        var (catalogo, validador) = await ObtenerCatalogoConPermisoAsync(claveCatalogo, rolUsuario);

        var contenidoNuevo  = JsonToBsonValue(request.Contenido);
        var contenidoLimpio = validador.Validar(contenidoNuevo, catalogo.Contenido);

        await _repository.UpdateContenidoAsync(catalogo.ClaveCatalogo, contenidoLimpio);

        catalogo.Contenido = contenidoLimpio;
        return _mapper.Map<CatalogoResponse>(catalogo);
    }

    // ─── Elementos individuales ─────────────────────────────────────────────

    /// <summary>
    /// Obtiene un solo elemento por su identificador. Disponible para cualquier rol autenticado.
    /// Ej: edificios/elementos/lab_c3, roles_sistema/elementos/tecnico
    /// </summary>
    public async Task<object?> GetElementoAsync(string claveCatalogo, string claveElemento)
    {
        var catalogo   = await ObtenerCatalogoAsync(claveCatalogo);
        var elementos  = ObtenerElementos(catalogo);
        var campoId    = _validadores.TryGetValue(catalogo.ClaveCatalogo, out var v) ? v.CampoIdentificador : IdentificadorPorDefecto;

        var indice = BuscarIndice(elementos, campoId, claveElemento);
        return BsonTypeMapper.MapToDotNetValue(elementos[indice]);
    }

    /// <summary>
    /// Agrega un elemento nuevo al catálogo. El backend lee lo actual, agrega el elemento
    /// y valida la lista completa con el mismo validador (claves repetidas, campos, etc.).
    /// </summary>
    public async Task<object?> AgregarElementoAsync(string claveCatalogo, ElementoCatalogoRequest request, string rolUsuario)
    {
        var (catalogo, validador) = await ObtenerCatalogoConPermisoAsync(claveCatalogo, rolUsuario, esAgregar: true);

        var elementos = ObtenerElementos(catalogo);
        var nuevo     = ElementoToBsonDocument(request);

        var listaNueva = new BsonArray(elementos) { nuevo };
        var indice     = listaNueva.Count - 1;

        var contenidoLimpio = ValidarConErroresDelElemento(validador, listaNueva, catalogo.Contenido, indice);
        await _repository.UpdateContenidoAsync(catalogo.ClaveCatalogo, contenidoLimpio);

        return BsonTypeMapper.MapToDotNetValue(contenidoLimpio.AsBsonArray[indice]);
    }

    /// <summary>
    /// Actualiza parcialmente un elemento (PATCH): solo cambian los campos enviados,
    /// los demás se conservan. El campo identificador (clave / rol) no se puede cambiar.
    /// </summary>
    public async Task<object?> ActualizarElementoAsync(string claveCatalogo, string claveElemento, ElementoCatalogoRequest request, string rolUsuario)
    {
        var (catalogo, validador) = await ObtenerCatalogoConPermisoAsync(claveCatalogo, rolUsuario);
        var elementos = ObtenerElementos(catalogo);
        var cambios   = ElementoToBsonDocument(request);

        var indice   = BuscarIndice(elementos, validador.CampoIdentificador, claveElemento);
        var original = elementos[indice].AsBsonDocument;
        var campoId  = validador.CampoIdentificador;

        // Mezclar: se copian los datos actuales y se sobrescriben solo los campos enviados
        var actualizado = original.DeepClone().AsBsonDocument;
        foreach (var campo in cambios)
        {
            if (campo.Name == campoId)
            {
                var idActual = original.Contains(campoId) && original[campoId].IsString ? original[campoId].AsString : string.Empty;
                if (!campo.Value.IsString || TextoNormalizer.NormalizarClave(campo.Value.AsString) != TextoNormalizer.NormalizarClave(idActual))
                    throw CatalogoErrors.IdentificadorNoEditable(campoId);
                continue; // mismo identificador: se conserva el original
            }
            actualizado[campo.Name] = campo.Value;
        }

        var listaNueva = new BsonArray(elementos);
        listaNueva[indice] = actualizado;

        var contenidoLimpio = ValidarConErroresDelElemento(validador, listaNueva, catalogo.Contenido, indice);
        await _repository.UpdateContenidoAsync(catalogo.ClaveCatalogo, contenidoLimpio);

        return BsonTypeMapper.MapToDotNetValue(contenidoLimpio.AsBsonArray[indice]);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>Busca el catálogo por clave (normalizada). Lanza 404 si no existe.</summary>
    private async Task<Catalogo> ObtenerCatalogoAsync(string claveCatalogo)
    {
        NoSqlSanitizer.Validar(claveCatalogo, "claveCatalogo");
        var claveNormalizada = TextoNormalizer.NormalizarClave(claveCatalogo);

        return await _repository.GetByClaveAsync(claveNormalizada)
            ?? throw CatalogoErrors.ClaveNotFound(claveNormalizada);
    }

    /// <summary>
    /// Para operaciones de escritura (PUT / POST / PATCH). Orden:
    ///   1) Si es POST, que el catálogo permita agregar (403 "no permite agregar", aplica a todos los roles).
    ///   2) PERMISOS: que el rol pueda editar el catálogo (403 "no tienes permiso"). Aún no se consulta la BD.
    ///   3) EXISTENCIA después: se busca el catálogo en Mongo (404 si no existe).
    /// Si la clave no tiene reglas (ej. "edificio" mal escrito) no se pueden conocer sus permisos:
    /// se responde 404 si no existe, o 403 "no se puede modificar" si existe pero no es editable.
    /// </summary>
    private async Task<(Catalogo Catalogo, ICatalogoValidator Validador)> ObtenerCatalogoConPermisoAsync(
        string claveCatalogo, string rolUsuario, bool esAgregar = false)
    {
        NoSqlSanitizer.Validar(claveCatalogo, "claveCatalogo");
        var claveNormalizada = TextoNormalizer.NormalizarClave(claveCatalogo);

        if (!_validadores.TryGetValue(claveNormalizada, out var validador))
        {
            _ = await ObtenerCatalogoAsync(claveCatalogo);        // 404 si no existe
            throw CatalogoErrors.NoEditable(claveNormalizada);   // existe, pero no es editable
        }

        // 1) ¿La operación existe para este catálogo? (ej. roles_sistema no permite agregar para NADIE)
        if (esAgregar && !validador.PermiteAgregarElementos)
            throw CatalogoErrors.NoPermiteAgregar();

        // 2) Permisos del rol
        if (!validador.RolesEditores.Contains(TextoNormalizer.SinAcentos(rolUsuario)))
            throw CatalogoErrors.SinPermiso(claveNormalizada, TextoNormalizer.Normalizar(rolUsuario));

        // 3) Existencia
        var catalogo = await ObtenerCatalogoAsync(claveCatalogo);
        return (catalogo, validador);
    }

    /// <summary>Los elementos solo existen cuando el contenido es un arreglo de objetos.</summary>
    private static BsonArray ObtenerElementos(Catalogo catalogo)
    {
        if (!catalogo.Contenido.IsBsonArray)
            throw CatalogoErrors.NoManejaElementos();
        return catalogo.Contenido.AsBsonArray;
    }

    /// <summary>Índice del elemento cuyo identificador coincide (ignora mayúsculas, espacios y acentos).</summary>
    private static int BuscarIndice(BsonArray elementos, string campoId, string claveElemento)
    {
        NoSqlSanitizer.Validar(claveElemento, "claveElemento");
        var buscado = TextoNormalizer.NormalizarClave(claveElemento);

        for (var i = 0; i < elementos.Count; i++)
        {
            if (elementos[i] is BsonDocument doc
                && doc.Contains(campoId)
                && doc[campoId].IsString
                && TextoNormalizer.NormalizarClave(doc[campoId].AsString) == buscado)
                return i;
        }
        throw CatalogoErrors.ElementoNoEncontrado(claveElemento.Trim());
    }

    /// <summary>
    /// Valida la lista completa, pero los errores del elemento enviado se devuelven con nombres
    /// de campo simples ("salon" en vez de "contenido[5].salon") para que el frontend
    /// marque directo el input del formulario.
    /// </summary>
    private static BsonValue ValidarConErroresDelElemento(ICatalogoValidator validador, BsonArray lista, BsonValue actual, int indice)
    {
        try
        {
            return validador.Validar(lista, actual);
        }
        catch (DomainException ex) when (ex.Errors is not null)
        {
            var prefijo = $"contenido[{indice}]";
            var errores = ex.Errors.Select(e =>
            {
                // También se limpia el mensaje: "ya existe en contenido[0].campos_dinamicos[0].clave" → "...en campos_dinamicos[0].clave"
                var mensaje = e.Mensaje.Replace(prefijo + ".", string.Empty);
                var campo   = e.Campo == prefijo                ? "elemento"
                            : e.Campo.StartsWith(prefijo + ".") ? e.Campo[(prefijo.Length + 1)..]
                            : e.Campo;
                return new ErrorDetalle(campo, mensaje);
            });

            throw CatalogoErrors.ElementoInvalido(errores);
        }
    }

    /// <summary>Convierte los campos recibidos a BsonDocument. Debe traer al menos un campo.</summary>
    private static BsonDocument ElementoToBsonDocument(ElementoCatalogoRequest request)
    {
        if (request.Campos is null || request.Campos.Count == 0)
            throw CatalogoErrors.ElementoVacio();

        try
        {
            return BsonDocument.Parse(JsonSerializer.Serialize(request.Campos));
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            throw CatalogoErrors.ElementoFormatoInvalido();
        }
    }

    /// <summary>
    /// Convierte el JSON recibido a BsonValue. Solo acepta array u object
    /// (igual que la validación de la colección en MongoDB).
    /// </summary>
    private static BsonValue JsonToBsonValue(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            throw CatalogoErrors.ContenidoObligatorio();

        if (element.ValueKind is not (JsonValueKind.Array or JsonValueKind.Object))
            throw CatalogoErrors.ContenidoTipoInvalido();

        try
        {
            var wrapper = BsonDocument.Parse($"{{\"v\":{element.GetRawText()}}}");
            return wrapper["v"];
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException)
        {
            throw CatalogoErrors.FormatoJsonInvalido();
        }
    }
}
