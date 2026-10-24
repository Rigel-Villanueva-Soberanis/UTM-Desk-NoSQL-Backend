using Application.Common;

namespace Application.Common.ErrorCatalog;

public static class AuthErrors
{
    public static DomainException CredencialesInvalidas() =>
        new(ApiErrors.Unauthorized, "Usuario o contraseña incorrectos.");

    public static DomainException CuentaInactiva() =>
        new(ApiErrors.Unauthorized, "La cuenta está desactivada. Contacta con el soporte.");
}
