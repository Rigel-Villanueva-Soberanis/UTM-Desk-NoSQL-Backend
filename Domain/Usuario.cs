using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain;

/// <summary>
/// Representa un usuario del sistema UTM Desk.
/// </summary>
public class Usuario
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    [BsonElement("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [BsonElement("usuario")]
    public string Username { get; set; } = string.Empty;

    [BsonElement("correo")]
    public string Correo { get; set; } = string.Empty;

    [BsonElement("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("estado_cuenta")]
    public string EstadoCuenta { get; set; } = "activo";

    [BsonElement("rol_sistema")]
    public string RolSistema { get; set; } = string.Empty;

    [BsonElement("clasificacion_rol")]
    public string ClasificacionRol { get; set; } = string.Empty;

    [BsonElement("fecha_creacion")]
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
