using Domain;
using MongoDB.Bson;

namespace Application.Interfaces;

/// <summary>
/// Contrato del repositorio de catálogos.
/// No hay Create ni Delete: las claves de catálogo no se crean ni se borran desde la API.
/// </summary>
public interface ICatalogoRepository
{
    Task<IEnumerable<Catalogo>> GetAllAsync();
    Task<Catalogo?> GetByClaveAsync(string clave);
    Task UpdateContenidoAsync(string clave, BsonValue contenido);
}
