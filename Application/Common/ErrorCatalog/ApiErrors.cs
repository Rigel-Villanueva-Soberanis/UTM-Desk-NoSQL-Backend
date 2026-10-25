namespace Application.Common.ErrorCatalog;

/// <summary>
/// Errores genéricos reutilizables en toda la aplicación.
/// </summary>
public static class ApiErrors
{
    public const string NotFound = "NOT_FOUND";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string InternalError = "INTERNAL_ERROR";
    public const string Conflict = "CONFLICT";

    /// <summary>Un servicio externo (ej. Cloudinary) no respondió o rechazó la operación → 503.</summary>
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
}
