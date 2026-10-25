using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsUsuarios;

public static class AuthErrors
{
    public static DomainException CredencialesInvalidas() =>
        new(ApiErrors.Unauthorized, "Usuario o contraseña incorrectos.");

    public static DomainException CuentaInactiva() =>
        new(ApiErrors.Unauthorized, "La cuenta está desactivada. Contacta con el soporte.");
}
