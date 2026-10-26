using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Domain;

/// <summary>
/// Representa una incidencia (ticket) del sistema UTM Desk.
/// Es un documento "expediente completo": todo lo del ticket vive embebido
/// para recuperarlo en una sola lectura por _id.
/// </summary>
[BsonIgnoreExtraElements]
public class Incidencia
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    /// <summary>Folio legible generado con Redis. Ej: INC-2026-0042.</summary>
    [BsonElement("folio")]
    public string Folio { get; set; } = string.Empty;

    [BsonElement("titulo")]
    public string Titulo { get; set; } = string.Empty;

    [BsonElement("descripcion")]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Clave de la categoría en el catálogo categorias_incidencia. Ej: "computo".</summary>
    [BsonElement("categoria")]
    public string Categoria { get; set; } = string.Empty;

    /// <summary>Datos dinámicos según los campos_dinamicos de la categoría.</summary>
    [BsonElement("detalle_especifico")]
    public BsonValue DetalleEspecifico { get; set; } = new BsonDocument();

    [BsonElement("estado")]
    public string Estado { get; set; } = "abierta";

    [BsonElement("prioridad")]
    public string Prioridad { get; set; } = "media";

    [BsonElement("ubicacion")]
    public UbicacionIncidencia Ubicacion { get; set; } = new();

    [BsonElement("datos_reportante")]
    public DatosReportante DatosReportante { get; set; } = new();

    [BsonElement("tecnico_asignado")]
    public UsuarioReferencia? TecnicoAsignado { get; set; }

    [BsonElement("tecnico_externo")]
    public TecnicoExterno? TecnicoExterno { get; set; }

    [BsonElement("horas_invertidas")]
    public double? HorasInvertidas { get; set; }

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("historial_seguimiento")]
    public List<SeguimientoIncidencia> HistorialSeguimiento { get; set; } = new();

    [BsonElement("evidencias_adjuntas")]
    public List<EvidenciaIncidencia> EvidenciasAdjuntas { get; set; } = new();

    [BsonElement("fechas")]
    public FechasIncidencia Fechas { get; set; } = new();
}

/// <summary>Ubicación tomada del catálogo edificios.</summary>
[BsonIgnoreExtraElements]
public class UbicacionIncidencia
{
    /// <summary>Clave del edificio en el catálogo edificios. Ej: "edificio_a".</summary>
    [BsonElement("clave")]
    [BsonIgnoreIfNull]
    public string? Clave { get; set; }

    [BsonElement("edificio")]
    public string Edificio { get; set; } = string.Empty;

    [BsonElement("aula")]
    public string? Aula { get; set; }
}

/// <summary>Quién levantó el reporte (se toma del token JWT).</summary>
[BsonIgnoreExtraElements]
public class DatosReportante
{
    [BsonElement("id_usuario")]
    public string IdUsuario { get; set; } = string.Empty;

    [BsonElement("nombre")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Clasificación del reportante. Ej: "docente", "estudiante".</summary>
    [BsonElement("perfil")]
    public string? Perfil { get; set; }
}

/// <summary>Referencia ligera a un usuario (técnico, autor de bitácora, quien sube evidencia).</summary>
[BsonIgnoreExtraElements]
public class UsuarioReferencia
{
    [BsonElement("id_usuario")]
    public string IdUsuario { get; set; } = string.Empty;

    [BsonElement("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [BsonElement("rol")]
    public string Rol { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public class TecnicoExterno
{
    [BsonElement("requiere_servicio_externo")]
    public bool RequiereServicioExterno { get; set; }

    [BsonElement("empresa_proveedor")]
    public string? EmpresaProveedor { get; set; }

    [BsonElement("nombre_tecnico")]
    public string? NombreTecnico { get; set; }

    [BsonElement("contacto")]
    public string? Contacto { get; set; }

    [BsonElement("orden_servicio_ref")]
    public string? OrdenServicioRef { get; set; }
}

/// <summary>Entrada de la bitácora de seguimiento.</summary>
[BsonIgnoreExtraElements]
public class SeguimientoIncidencia
{
    /// <summary>
    /// Tipo de evento: creacion, evidencia, asignacion, reasignacion, cambio_estado, nota, horas,
    /// etiquetas, servicio_externo, edicion.
    /// "nota" es la única manual (la escribe el técnico); las demás las genera el sistema.
    /// </summary>
    [BsonElement("tipo")]
    [BsonIgnoreIfNull]
    public string? Tipo { get; set; }

    [BsonElement("autor")]
    public UsuarioReferencia Autor { get; set; } = new();

    [BsonElement("accion")]
    public string Accion { get; set; } = string.Empty;

    /// <summary>Estado antes del evento (solo en cambios de estado).</summary>
    [BsonElement("estado_anterior")]
    [BsonIgnoreIfNull]
    public string? EstadoAnterior { get; set; }

    /// <summary>Estado de la incidencia después del evento.</summary>
    [BsonElement("estado_reportado")]
    public string? EstadoReportado { get; set; }

    [BsonElement("fecha")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
public class EvidenciaIncidencia
{
    [BsonElement("nombre_archivo")]
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>URL del archivo en Cloudinary.</summary>
    [BsonElement("url_storage")]
    public string UrlStorage { get; set; } = string.Empty;

    /// <summary>Identificador del archivo en Cloudinary (sirve para administrarlo o borrarlo).</summary>
    [BsonElement("public_id")]
    [BsonIgnoreIfNull]
    public string? PublicId { get; set; }

    [BsonElement("formato")]
    [BsonIgnoreIfNull]
    public string? Formato { get; set; }

    [BsonElement("subido_por")]
    public UsuarioReferencia? SubidoPor { get; set; }

    [BsonElement("fecha_carga")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? FechaCarga { get; set; }
}

/// <summary>
/// Fechas del ciclo de vida. Se guardan como BSON Date (ISODate):
/// "resolucion" es la que usa el índice TTL de borrado automático.
/// </summary>
[BsonIgnoreExtraElements]
public class FechasIncidencia
{
    [BsonElement("creacion")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime Creacion { get; set; } = DateTime.UtcNow;

    [BsonElement("resolucion")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? Resolucion { get; set; }
}
