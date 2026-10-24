using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de catálogos usando MongoDB.Driver.
/// </summary>
public class CatalogoRepository : ICatalogoRepository
{
    private readonly IMongoCollection<Catalogo> _collection;

    public CatalogoRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<Catalogo>("catalogos");
    }

    public async Task<IEnumerable<Catalogo>> GetAllAsync()
        => await _collection.Find(_ => true).ToListAsync();

    public async Task<Catalogo?> GetByClaveAsync(string clave)
        => await _collection.Find(c => c.ClaveCatalogo == clave).FirstOrDefaultAsync();

    /// <summary>
    /// Reemplaza solo el campo <c>contenido</c>. El _id y la clave_catalogo nunca se tocan.
    /// </summary>
    public async Task UpdateContenidoAsync(string clave, BsonValue contenido)
        => await _collection.UpdateOneAsync(
            c => c.ClaveCatalogo == clave,
            Builders<Catalogo>.Update.Set(c => c.Contenido, contenido));
}
