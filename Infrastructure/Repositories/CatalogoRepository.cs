using Application.Interfaces;
using Domain;
using Infrastructure.Data;
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

    public async Task<Catalogo?> GetByIdAsync(string id)
        => await _collection.Find(c => c.Id == id).FirstOrDefaultAsync();

    public async Task<Catalogo?> GetByClaveAsync(string clave)
        => await _collection.Find(c => c.ClaveCatalogo == clave).FirstOrDefaultAsync();

    public async Task<Catalogo> CreateAsync(Catalogo catalogo)
    {
        await _collection.InsertOneAsync(catalogo);
        return catalogo;
    }

    public async Task UpdateAsync(Catalogo catalogo)
        => await _collection.ReplaceOneAsync(c => c.Id == catalogo.Id, catalogo);

    public async Task DeleteAsync(string id)
        => await _collection.DeleteOneAsync(c => c.Id == id);
}
