using Application.Common.ErrorCatalog;
using Application.Common.ErrorsUsuarios;
using Application.Common.Utilidades;
using Application.DTOs.Requests;
using Application.Interfaces;

namespace Application.Validators.Usuarios;

/// <summary>
/// Reglas de validación de los filtros de usuarios.
/// Los mensajes de error están en <see cref="UsuarioErrors"/>.
/// </summary>
public static class UsuarioValidator
{
    /// <summary>Mismos valores que el enum de estado_cuenta en la validación de MongoDB.</summary>
    public static readonly string[] EstadosCuenta = ["activo", "inactivo", "bloqueado"];

    /// <summary>Filtros de GET /api/Usuario.</summary>
    public static UsuarioConsulta ValidarFiltros(UsuarioFiltroRequest filtro)
    {
        NoSqlSanitizer.ValidarTodos(
            (filtro.Busqueda,      "busqueda"),
            (filtro.Nombres,       "nombres"),
            (filtro.Apellidos,     "apellidos"),
            (filtro.Rol,           "rol"),
            (filtro.Clasificacion, "clasificacion"),
            (filtro.EstadoCuenta,  "estadoCuenta")
        );

        var errores = new List<ErrorDetalle>();

        var estado = Texto(filtro.EstadoCuenta);
        if (estado is not null && !EstadosCuenta.Contains(estado))
        {
            errores.Add(UsuarioErrors.EstadoCuentaNoValido(filtro.EstadoCuenta, EstadosCuenta));
        }

        if (errores.Count > 0)
            throw UsuarioErrors.FiltrosInvalidos(errores);

        return new UsuarioConsulta(
            filtro.Pagina, filtro.TamanoPagina,
            Texto(filtro.Busqueda), Texto(filtro.Nombres), Texto(filtro.Apellidos),
            Texto(filtro.Rol), Texto(filtro.Clasificacion), estado);
    }

    /// <summary>Filtros de GET /api/Usuario/tecnicos (siempre técnicos activos; sin paginar).</summary>
    public static UsuarioConsulta ValidarFiltrosTecnicos(TecnicoFiltroRequest filtro)
    {
        NoSqlSanitizer.ValidarTodos(
            (filtro.Busqueda,      "busqueda"),
            (filtro.Nombres,       "nombres"),
            (filtro.Apellidos,     "apellidos"),
            (filtro.Clasificacion, "clasificacion")
        );

        return new UsuarioConsulta(
            1, int.MaxValue,
            Texto(filtro.Busqueda), Texto(filtro.Nombres), Texto(filtro.Apellidos),
            null, Texto(filtro.Clasificacion), null);
    }

    /// <summary>Texto normalizado (minúsculas, sin espacios sobrantes) o null si viene vacío.</summary>
    private static string? Texto(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : TextoNormalizer.Normalizar(valor);
}
