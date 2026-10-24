using Application.Common;

namespace Application.Common.ErrorCatalog;

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
        new(ApiErrors.ValidationFailed, $"La clasificación '{clasificacion}' no es válida para el rol '{rol}'. Clasificaciones permitidas: {string.Join(", ", permitidas)}.");

    public static DomainException PasswordIncorrecta() =>
        new(ApiErrors.Unauthorized, "La contraseña actual es incorrecta.");
}
