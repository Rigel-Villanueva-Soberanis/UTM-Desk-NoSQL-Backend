using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain;

/// <summary>
/// Representa un catálogo del sistema.
/// El campo Contenido es dinámico: puede ser una lista de strings
/// o una lista de objetos, dependiendo del tipo de catálogo.
/// </summary>
public class Catalogo
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("clave_catalogo")]
    public string ClaveCatalogo { get; set; } = string.Empty;

    /// <summary>
    /// Contenido dinámico: puede ser array de strings u objetos complejos.
    /// BsonArray permite almacenar cualquier estructura de MongoDB sin restricción de tipo.
    /// </summary>
    [BsonElement("contenido")]
    public BsonArray Contenido { get; set; } = new BsonArray();
}
