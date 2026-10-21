namespace Application.Common;

/// <summary>
/// Excepciones controladas del dominio de negocio.
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
