using Application.Interfaces;
using Domain;
using Infrastructure.Data;
using MongoDB.Driver;

namespace Infrastructure.Repositories;

/// <summary>
/// Implementación MongoDB del repositorio de usuarios.
/// Usa la colección "usuarios" de la base de datos configurada en appsettings.
/// </summary>
public class UsuarioRepository : IUsuarioRepository
{
    private readonly IMongoCollection<Usuario> _collection;

    public UsuarioRepository(MongoDbContext context)
    {
        _collection = context.GetCollection<Usuario>("usuarios");
    }

    public async Task<IEnumerable<Usuario>> GetAllAsync()
        => await _collection.Find(_ => true).ToListAsync();

    public async Task<Usuario?> GetByIdAsync(string id)
        => await _collection.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task<Usuario?> GetByUsernameAsync(string username)
        => await _collection.Find(u => u.Username == username).FirstOrDefaultAsync();

    public async Task<Usuario?> GetByCorreoAsync(string correo)
        => await _collection.Find(u => u.Correo == correo).FirstOrDefaultAsync();

    public async Task<Usuario> CreateAsync(Usuario usuario)
    {
        await _collection.InsertOneAsync(usuario);
        return usuario;
    }

    public async Task UpdateAsync(Usuario usuario)
        => await _collection.ReplaceOneAsync(u => u.Id == usuario.Id, usuario);

    public async Task DeleteAsync(string id)
        => await _collection.DeleteOneAsync(u => u.Id == id);
}
