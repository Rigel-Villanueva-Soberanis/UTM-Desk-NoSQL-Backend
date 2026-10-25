using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>
/// Filtros para GET /api/Usuario (administración de cuentas).
/// Los textos se buscan parcialmente y sin importar mayúsculas ni acentos ("jose" encuentra "José").
/// </summary>
public class UsuarioFiltroRequest
{
    /// <summary>Número de página (inicia en 1).</summary>
    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "La página debe ser 1 o mayor.")]
    public int Pagina { get; set; } = 1;

    /// <summary>Registros por página (máximo 50).</summary>
    /// <example>10</example>
    [Range(1, 50, ErrorMessage = "El tamaño de página debe estar entre 1 y 50.")]
    public int TamanoPagina { get; set; } = 10;

    /// <summary>Busca en nombres, apellidos, usuario y correo a la vez.</summary>
    /// <example>carlos</example>
    public string? Busqueda { get; set; }

    /// <summary>Busca solo en nombres.</summary>
    /// <example>Carlos</example>
    public string? Nombres { get; set; }

    /// <summary>Busca en apellido paterno o materno.</summary>
    /// <example>Dzib</example>
    public string? Apellidos { get; set; }

    /// <summary>Rol exacto: root, administrativos, soporte, técnico, reportante, directivos.</summary>
    /// <example>técnico</example>
    public string? Rol { get; set; }

    /// <summary>Clasificación exacta (subrol). Ej: docente, estudiante, redes.</summary>
    /// <example>redes</example>
    public string? Clasificacion { get; set; }

    /// <summary>activo, inactivo o bloqueado.</summary>
    /// <example>activo</example>
    public string? EstadoCuenta { get; set; }
}
