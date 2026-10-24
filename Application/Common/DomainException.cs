namespace Application.Common;

/// <summary>
/// Excepciones controladas del dominio de negocio.
/// Opcionalmente lleva errores por campo (ej. validación de catálogos) para el frontend.
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    /// <summary>Errores por campo. Null cuando solo hay un mensaje general.</summary>
    public IReadOnlyList<ErrorDetalle>? Errors { get; }

    public DomainException(string code, string message, IEnumerable<ErrorDetalle>? errors = null) : base(message)
    {
        Code   = code;
        Errors = errors?.ToList();
    }
}
