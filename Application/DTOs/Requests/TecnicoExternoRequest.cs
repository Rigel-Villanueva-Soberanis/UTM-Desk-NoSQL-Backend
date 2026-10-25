using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Requests;

/// <summary>Datos del proveedor externo que atenderá la incidencia (subcontratación).</summary>
public class TecnicoExternoRequest
{
    /// <summary>Empresa proveedora.</summary>
    /// <example>Telmex</example>
    [Required(ErrorMessage = "La empresa es obligatoria.")]
    [StringLength(100, ErrorMessage = "La empresa no puede tener más de 100 caracteres.")]
    public string Empresa { get; set; } = null!;

    /// <summary>Técnico responsable por parte del proveedor (opcional).</summary>
    /// <example>Ing. Roberto Pech</example>
    [StringLength(100, ErrorMessage = "El técnico responsable no puede tener más de 100 caracteres.")]
    public string? TecnicoResponsable { get; set; }

    /// <summary>Teléfono o correo de contacto del proveedor (opcional).</summary>
    /// <example>999 123 4567</example>
    [StringLength(100, ErrorMessage = "El contacto no puede tener más de 100 caracteres.")]
    public string? Contacto { get; set; }

    /// <summary>Número de orden de servicio del proveedor.</summary>
    /// <example>OS-8841</example>
    [Required(ErrorMessage = "La orden de servicio es obligatoria.")]
    [StringLength(50, ErrorMessage = "La orden de servicio no puede tener más de 50 caracteres.")]
    public string OrdenServicio { get; set; } = null!;
}
