namespace Application.Common;

/// <summary>
/// Roles del sistema tal como viajan en el token (minúsculas).
/// Las comparaciones en código se hacen con <see cref="Es"/>, que ignora acentos
/// (el claim de técnico viene como "técnico").
/// </summary>
public static class RolesSistema
{
    public const string Root            = "root";
    public const string Reportante      = "reportante";
    public const string Tecnico         = "tecnico";
    public const string Soporte         = "soporte";
    public const string Directivos      = "directivos";
    public const string Administrativos = "administrativos";

    /// <summary>¿El rol del usuario es el esperado? Ignora mayúsculas y acentos.</summary>
    public static bool Es(string? rolUsuario, string rolEsperado)
        => TextoNormalizer.SinAcentos(rolUsuario) == rolEsperado;
}
