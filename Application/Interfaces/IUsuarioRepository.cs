using Domain;

namespace Application.Interfaces;

/// <summary>
/// Contrato del repositorio de usuarios.
/// </summary>
public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> GetAllAsync();

    /// <summary>Usuarios filtrados y paginados, ordenados por nombre.</summary>
    Task<(IReadOnlyList<Usuario> Items, long Total)> GetPaginadoAsync(UsuarioConsulta consulta);

    /// <summary>Solo técnicos con cuenta activa (los que se pueden asignar), filtrados y ordenados por nombre.</summary>
    Task<IReadOnlyList<Usuario>> GetTecnicosActivosAsync(UsuarioConsulta consulta);
    Task<Usuario?> GetByIdAsync(string id);
    Task<Usuario?> GetByUsernameAsync(string username);
    Task<Usuario?> GetByCorreoAsync(string correo);
    Task<Usuario> CreateAsync(Usuario usuario);
    Task UpdateAsync(Usuario usuario);
    Task DeleteAsync(string id);
}

/// <summary>
/// Filtros de usuarios ya validados. Los textos se buscan parcialmente, sin importar mayúsculas ni acentos.
/// </summary>
public record UsuarioConsulta(
    int Pagina,
    int TamanoPagina,
    string? Busqueda,      // en nombres, apellidos, usuario o correo
    string? Nombres,
    string? Apellidos,     // paterno o materno
    string? Rol,           // exacto (sin acentos): "tecnico" encuentra "técnico"
    string? Clasificacion, // exacta (sin acentos)
    string? EstadoCuenta); // activo, inactivo o bloqueado
