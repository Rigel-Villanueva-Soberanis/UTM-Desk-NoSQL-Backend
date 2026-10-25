namespace Application.Common;

/// <summary>
/// Datos del usuario autenticado extraídos del token JWT.
/// El controller lo arma y se lo pasa al servicio, así Application no depende de HttpContext.
/// </summary>
/// <param name="Id">ObjectId del usuario (claim "sub").</param>
/// <param name="Nombre">Nombre completo (claim "nombre_completo").</param>
/// <param name="Rol">Rol en minúsculas (ej. "reportante", "técnico").</param>
/// <param name="Clasificacion">Subrol (ej. "docente"). Null si el rol no maneja clasificación.</param>
public record UsuarioActual(string Id, string Nombre, string Rol, string? Clasificacion)
{
    public bool EsRol(string rol) => RolesSistema.Es(Rol, rol);
}
