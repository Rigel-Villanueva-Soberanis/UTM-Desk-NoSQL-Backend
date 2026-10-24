using Domain;

namespace Application.Interfaces;

/// <summary>
/// Contrato del repositorio de usuarios.
/// </summary>
public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(string id);
    Task<Usuario?> GetByUsernameAsync(string username);
    Task<Usuario?> GetByCorreoAsync(string correo);
    Task<Usuario> CreateAsync(Usuario usuario);
    Task UpdateAsync(Usuario usuario);
    Task DeleteAsync(string id);
}
