using Domain;

namespace Application.Interfaces;

/// <summary>
/// Contrato del repositorio de catálogos.
/// </summary>
public interface ICatalogoRepository
{
    Task<IEnumerable<Catalogo>> GetAllAsync();
    Task<Catalogo?> GetByIdAsync(string id);
    Task<Catalogo?> GetByClaveAsync(string clave);
    Task<Catalogo> CreateAsync(Catalogo catalogo);
    Task UpdateAsync(Catalogo catalogo);
    Task DeleteAsync(string id);
}
