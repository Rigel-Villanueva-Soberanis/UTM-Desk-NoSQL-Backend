using Domain;

namespace Application.Interfaces;

/// <summary>
/// Contrato del repositorio de ejemplo.
/// Crea una interfaz similar por cada entidad del dominio.
/// </summary>
public interface IEntityRepository
{
    Task<IEnumerable<Entity>> GetAllAsync();
    Task<Entity?> GetByIdAsync(int id);
    Task<int> CreateAsync(Entity entity);
    Task UpdateAsync(Entity entity);
    Task DeleteAsync(int id);
}
