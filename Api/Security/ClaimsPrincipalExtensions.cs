using Application.Common.ErrorCatalog;
using Application.Common.Seguridad;
using Application.Common.Utilidades;
using System.Security.Claims;

namespace Api.Security;

/// <summary>
/// Lee del token JWT los datos del usuario autenticado.
/// Uso en un controller: <c>var usuario = User.ObtenerUsuarioActual();</c>
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public static UsuarioActual ObtenerUsuarioActual(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier)
              ?? user.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException(ApiErrors.Unauthorized, "No se pudo identificar al usuario desde el token.");

        var rol = user.FindFirstValue(ClaimTypes.Role)
               ?? user.FindFirstValue("rol_sistema")
               ?? string.Empty;

        // Tokens anteriores no traen "nombre_completo": se usa el nombre de usuario
        var nombre = user.FindFirstValue("nombre_completo")
                  ?? user.FindFirstValue(ClaimTypes.Name)
                  ?? user.FindFirstValue("unique_name")
                  ?? id;

        var clasificacion = user.FindFirstValue("clasificacion_rol");

        return new UsuarioActual(
            id,
            nombre,
            TextoNormalizer.Normalizar(rol),
            string.IsNullOrWhiteSpace(clasificacion) ? null : clasificacion);
    }
}
