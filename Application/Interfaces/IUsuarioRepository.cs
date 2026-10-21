using Domain;

namespace Application.Interfaces;

/// <summary>
/// Contrato del repositorio de usuarios.
/// </summary>
public interface IUsuarioRepository
{
    Task<Usuario?> GetByUsernameAsync(string username);
    Task<Usuario?> GetByIdAsync(string id);
}
