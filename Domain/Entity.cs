using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain;

/// <summary>
/// Entidad de ejemplo del dominio.
/// Crea una clase similar por cada objeto de negocio.
/// </summary>
public class Entity
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string Name { get; set; } = string.Empty;
}
