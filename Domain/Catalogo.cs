using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Domain;

/// <summary>
/// Representa un catálogo del sistema.
/// El campo Contenido es dinámico: según el catálogo puede ser un arreglo (array)
/// o una estructura de configuración (object), tal como lo permite la validación de MongoDB.
/// </summary>
public class Catalogo
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("clave_catalogo")]
    public string ClaveCatalogo { get; set; } = string.Empty;

    /// <summary>
    /// Contenido dinámico: BsonArray o BsonDocument.
    /// BsonValue permite leer cualquiera de las dos estructuras sin romper la deserialización.
    /// </summary>
    [BsonElement("contenido")]
    public BsonValue Contenido { get; set; } = new BsonArray();
}
