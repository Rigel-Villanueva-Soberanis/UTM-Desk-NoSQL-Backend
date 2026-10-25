namespace Application.DTOs.Responses;

/// <summary>Expediente completo de una incidencia.</summary>
public class IncidenciaResponse
{
    public string Id { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
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
    public double? HorasInvertidas { get; set; }
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
    public string UrlStorage { get; set; } = string.Empty;
    public string? PublicId { get; set; }
    public string? Formato { get; set; }
    public UsuarioReferenciaResponse? SubidoPor { get; set; }
    public DateTime? FechaCarga { get; set; }
}

public class FechasResponse
{
    public DateTime Creacion { get; set; }
    public DateTime? Resolucion { get; set; }
}
