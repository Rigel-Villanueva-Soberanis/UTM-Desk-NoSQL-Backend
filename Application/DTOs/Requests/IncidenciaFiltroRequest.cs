using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>
/// Parámetros de consulta (query string) para listar incidencias.
/// Ej: GET /api/incidencias?pagina=1&amp;tamanoPagina=10&amp;estado=abierta
/// </summary>
public class IncidenciaFiltroRequest
{
    /// <summary>Número de página (inicia en 1).</summary>
    /// <example>1</example>
    [Range(1, int.MaxValue, ErrorMessage = "La página debe ser 1 o mayor.")]
    public int Pagina { get; set; } = 1;

    /// <summary>Registros por página (máximo 50).</summary>
    /// <example>10</example>
    [Range(1, 50, ErrorMessage = "El tamaño de página debe estar entre 1 y 50.")]
    public int TamanoPagina { get; set; } = 10;

    /// <summary>abierta, en proceso, por autorizar, resuelta o cancelada.</summary>
    /// <example>abierta</example>
    public string? Estado { get; set; }

    /// <summary>baja, media, alta o critica.</summary>
    /// <example>alta</example>
    public string? Prioridad { get; set; }

    /// <summary>Clave de la categoría.</summary>
    /// <example>computo</example>
    public string? Categoria { get; set; }

    /// <summary>true = solo con técnico asignado · false = solo sin asignar · vacío = todas.</summary>
    /// <example>false</example>
    public bool? Asignada { get; set; }

    /// <summary>Nombre del edificio (tal como aparece en el catálogo edificios).</summary>
    /// <example>Edificio C</example>
    public string? Edificio { get; set; }

    /// <summary>Etiqueta exacta (sin importar mayúsculas ni acentos).</summary>
    /// <example>redes</example>
    public string? Tag { get; set; }

    /// <summary>true = solo las canalizadas con proveedor externo · false = solo sin proveedor · vacío = todas.</summary>
    /// <example>true</example>
    public bool? ConProveedorExterno { get; set; }

    /// <summary>Creadas desde este día (hora de México, incluido). Formato AAAA-MM-DD.</summary>
    /// <example>2026-10-01</example>
    public DateOnly? FechaDesde { get; set; }

    /// <summary>Creadas hasta este día (hora de México, incluido). Formato AAAA-MM-DD.</summary>
    /// <example>2026-10-31</example>
    public DateOnly? FechaHasta { get; set; }
}
