using Application.Common.ErrorCatalog;
using Application.DTOs.Requests;
using Application.DTOs.Responses;
using Application.Interfaces;
using AutoMapper;
using Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Text.Json;

namespace Application.Services;

/// <summary>
/// Casos de uso para la colección de catálogos.
/// </summary>
public class CatalogoService
{
    private readonly ICatalogoRepository _repository;
    private readonly IMapper _mapper;

    public CatalogoService(ICatalogoRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CatalogoResponse>> GetAllAsync()
    {
        var catalogos = await _repository.GetAllAsync();
        return _mapper.Map<IEnumerable<CatalogoResponse>>(catalogos);
    }

    public async Task<CatalogoResponse?> GetByIdAsync(string id)
    {
        var catalogo = await _repository.GetByIdAsync(id)
            ?? throw CatalogoErrors.NotFound(id);
        return _mapper.Map<CatalogoResponse>(catalogo);
    }

    public async Task<CatalogoResponse?> GetByClaveAsync(string clave)
    {
        var catalogo = await _repository.GetByClaveAsync(clave)
            ?? throw CatalogoErrors.ClaveNotFound(clave);
        return _mapper.Map<CatalogoResponse>(catalogo);
    }

    public async Task<CatalogoResponse> CreateAsync(CreateCatalogoRequest request)
    {
        // Validar que la clave no esté duplicada
        var existente = await _repository.GetByClaveAsync(request.ClaveCatalogo);
        if (existente is not null)
            throw CatalogoErrors.ClaveRepetida(request.ClaveCatalogo);

        var catalogo = new Catalogo
        {
            ClaveCatalogo = request.ClaveCatalogo,
            Contenido     = JsonToBsonArray(request.Contenido)
        };

        var creado = await _repository.CreateAsync(catalogo);
        return _mapper.Map<CatalogoResponse>(creado);
    }

    public async Task UpdateAsync(string id, UpdateCatalogoRequest request)
    {
        var catalogo = await _repository.GetByIdAsync(id)
            ?? throw CatalogoErrors.NotFound(id);

        // Si cambia la clave, verificar que no exista ya en otro documento
        if (catalogo.ClaveCatalogo != request.ClaveCatalogo)
        {
            var existente = await _repository.GetByClaveAsync(request.ClaveCatalogo);
            if (existente is not null)
                throw CatalogoErrors.ClaveRepetida(request.ClaveCatalogo);
        }

        catalogo.ClaveCatalogo = request.ClaveCatalogo;
        catalogo.Contenido     = JsonToBsonArray(request.Contenido);

        await _repository.UpdateAsync(catalogo);
    }

    public async Task DeleteAsync(string id)
    {
        var existe = await _repository.GetByIdAsync(id)
            ?? throw CatalogoErrors.NotFound(id);
        await _repository.DeleteAsync(id);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Convierte un JsonElement (recibido del cliente) a BsonArray (formato de MongoDB).
    /// Acepta cualquier estructura JSON sin importar si son strings u objetos.
    /// </summary>
    private static BsonArray JsonToBsonArray(JsonElement element)
    {
        var json = element.GetRawText();
        // Envuelve el array en un documento temporal para parsearlo con MongoDB
        var wrapper = BsonDocument.Parse($"{{\"v\":{json}}}");
        return wrapper["v"].AsBsonArray;
    }
}
