using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

/// <summary>
/// Implementación concreta del repositorio de ejemplo usando MongoDB.Driver.
/// Crea un repositorio similar por cada entidad del dominio.
/// </summary>
public class EntityRepository : IEntityRepository
{
    private readonly IMongoCollection<Entity> _collection;

    public EntityRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<Entity>("entities");
    }

    public async Task<IEnumerable<Entity>> GetAllAsync()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public async Task<Entity?> GetByIdAsync(string id)
    {
        return await _collection.Find(e => e.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Entity> CreateAsync(Entity entity)
    {
        await _collection.InsertOneAsync(entity);
        return entity;
    }

    public async Task UpdateAsync(Entity entity)
    {
        await _collection.ReplaceOneAsync(e => e.Id == entity.Id, entity);
    }

    public async Task DeleteAsync(string id)
    {
        await _collection.DeleteOneAsync(e => e.Id == id);
    }
}
