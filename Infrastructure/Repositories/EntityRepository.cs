using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using Dapper;

namespace Infrastructure.Repositories;

/// <summary>
/// Implementación concreta del repositorio de ejemplo usando Dapper.
/// Crea un repositorio similar por cada entidad del dominio.
/// </summary>
public class EntityRepository : IEntityRepository
{
    private readonly DbConnectionFactory _factory;

    public EntityRepository(DbConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<Entity>> GetAllAsync()
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryAsync<Entity>("SELECT * FROM Entities");
    }

    public async Task<Entity?> GetByIdAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Entity>(
            "SELECT * FROM Entities WHERE Id = @Id", new { Id = id });
    }

    public async Task<int> CreateAsync(Entity entity)
    {
        using var conn = _factory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO Entities (Name) OUTPUT INSERTED.Id VALUES (@Name)", entity);
    }

    public async Task UpdateAsync(Entity entity)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync("UPDATE Entities SET Name = @Name WHERE Id = @Id", entity);
    }

    public async Task DeleteAsync(int id)
    {
        using var conn = _factory.CreateConnection();
        await conn.ExecuteAsync("DELETE FROM Entities WHERE Id = @Id", new { Id = id });
    }
}
