namespace Application.DTOs.Responses;

/// <summary>
/// DTO de ejemplo para devolver un recurso genérico al cliente.
/// Duplica este archivo por cada entidad del dominio.
/// </summary>
public class EntityResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // Agrega las propiedades necesarias
}
