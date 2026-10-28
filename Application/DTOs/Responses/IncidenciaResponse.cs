namespace Application.DTOs.Responses;

/// <summary>Expediente completo de una incidencia.</summary>
public class IncidenciaResponse
{
    public string Id { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;

    /// <summary>Número que obtuvo en el contador del día y su categoría (ej. 3 = la 3.ª de esa categoría ese día). Null si Redis no lo dio.</summary>
    public long? ConsecutivoDia { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public object? DetalleEspecifico { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public UbicacionResponse Ubicacion { get; set; } = new();
    public DatosReportanteResponse DatosReportante { get; set; } = new();
    public UsuarioReferenciaResponse? TecnicoAsignado { get; set; }
    public TecnicoExternoResponse? TecnicoExterno { get; set; }
    /// <summary>Tiempo total invertido, en horas y minutos (0 si aún no se registra nada).</summary>
    public DuracionResponse TiempoInvertido { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<SeguimientoResponse> HistorialSeguimiento { get; set; } = new();
    public List<EvidenciaResponse> EvidenciasAdjuntas { get; set; } = new();
    public FechasResponse Fechas { get; set; } = new();
}

public class UbicacionResponse
{
    public string? Clave { get; set; }
    public string Edificio { get; set; } = string.Empty;
    public string? Aula { get; set; }
}

public class DatosReportanteResponse
{
    public string IdUsuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Perfil { get; set; }
}

public class UsuarioReferenciaResponse
{
    public string IdUsuario { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}

public class TecnicoExternoResponse
{
    public bool RequiereServicioExterno { get; set; }
    public string? EmpresaProveedor { get; set; }
    public string? NombreTecnico { get; set; }
    public string? Contacto { get; set; }
    public string? OrdenServicioRef { get; set; }
}

public class SeguimientoResponse
{
    /// <summary>creacion, evidencia, asignacion, reasignacion, cambio_estado, nota (manual), horas, etiquetas, servicio_externo o edicion.</summary>
    public string? Tipo { get; set; }
    public UsuarioReferenciaResponse Autor { get; set; } = new();
    public string Accion { get; set; } = string.Empty;
    public string? EstadoAnterior { get; set; }
    public string? EstadoReportado { get; set; }
    public DateTime Fecha { get; set; }
}

public class EvidenciaResponse
{
    public string NombreArchivo { get; set; } = string.Empty;

    /// <summary>
    /// Enlace firmado para ver o descargar el archivo. Dura pocos minutos (ver <see cref="VenceEn"/>):
    /// no lo guardes ni lo compartas; si venció, vuelve a pedir el expediente y trae uno nuevo.
    /// </summary>
    public string UrlTemporal { get; set; } = string.Empty;

    /// <summary>Instante (UTC) en que deja de funcionar <see cref="UrlTemporal"/>. Null si el archivo es antiguo y no tiene enlace temporal.</summary>
    public DateTime? VenceEn { get; set; }

    public string? PublicId { get; set; }
    public string? Formato { get; set; }

    /// <summary>Tipo de contenido. Ej: image/webp, application/pdf. Null en evidencias antiguas.</summary>
    public string? TipoMime { get; set; }

    /// <summary>Peso en kilobytes (2 decimales). Null en evidencias antiguas.</summary>
    public double? PesoKb { get; set; }

    public UsuarioReferenciaResponse? SubidoPor { get; set; }
    public DateTime? FechaCarga { get; set; }
}

public class FechasResponse
{
    public DateTime Creacion { get; set; }
    public DateTime? Resolucion { get; set; }
}
