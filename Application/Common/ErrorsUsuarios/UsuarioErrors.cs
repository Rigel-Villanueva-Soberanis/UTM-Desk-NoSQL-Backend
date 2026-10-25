using Application.Common.ErrorCatalog;

namespace Application.Common.ErrorsUsuarios;

public static class UsuarioErrors
{
    public static DomainException NotFound ()=>
        new(ApiErrors.NotFound, "El usuario no fue encontrado.");

    public static DomainException CorreoRepetido(string correo) =>
        new(ApiErrors.Conflict, $"Ya existe un usuario con el correo '{correo}'.");

    public static DomainException UsernameRepetido(string username) =>
        new(ApiErrors.Conflict, $"Ya existe un usuario con el nombre de usuario '{username}'.");

    public static DomainException RolNoExiste(string rol) =>
        new(ApiErrors.ValidationFailed, $"El rol '{rol}' no existe. Verifica que sea uno de los roles registrados en el catálogo.");

    public static DomainException ClasificacionNoValida(string clasificacion, string rol, IEnumerable<string> permitidas) =>
        new(ApiErrors.ValidationFailed, $"La clasificación '{clasificacion}' no es válida para el rol '{rol}'.");

    public static DomainException ClasificacionRequerida(string rol, IEnumerable<string> permitidas) =>
        new(ApiErrors.ValidationFailed, $"El rol '{rol}' requiere una clasificación.");

    public static DomainException ClasificacionNoAplica(string rol) =>
        new(ApiErrors.ValidationFailed, $"El rol '{rol}' no maneja clasificaciones. Envía la clasificación vacía.");

    public static DomainException FiltrosInvalidos(IEnumerable<ErrorDetalle> errores) =>
        new(ApiErrors.ValidationFailed, "Los filtros de búsqueda no son válidos. Revisa los errores.", errores);

    public static ErrorDetalle EstadoCuentaNoValido(string? valor, IEnumerable<string> opciones) =>
        new("estadoCuenta", $"'{valor}' no es válido. Opciones: {string.Join(", ", opciones)}.");

    public static DomainException PasswordIncorrecta() =>
        new(ApiErrors.Unauthorized, "La contraseña actual es incorrecta.");
}
