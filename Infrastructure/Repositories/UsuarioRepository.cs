using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de usuarios usando MongoDB.Driver.
/// </summary>
public class UsuarioRepository : IUsuarioRepository
{
    private readonly IMongoCollection<Usuario> _collection;

    public UsuarioRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<Usuario>("usuarios");
    }

    public async Task<Usuario?> GetByUsernameAsync(string username)
        => await _collection.Find(u => u.Username == username).FirstOrDefaultAsync();

    public async Task<Usuario?> GetByIdAsync(string id)
        => await _collection.Find(u => u.Id == id).FirstOrDefaultAsync();
}
