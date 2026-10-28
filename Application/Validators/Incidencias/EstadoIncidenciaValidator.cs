using Application.Common.ErrorsIncidencias;
using Application.Common.Seguridad;
using Application.Common.Utilidades;

namespace Application.Validators.Incidencias;

/// <summary>
/// Reglas para cambiar el estado de una incidencia.
/// Flujo normal: abierta → en proceso ⇄ en espera → resuelta → cerrada (o cancelada).
///   • Root, Soporte y Administrativos: pueden hacer cualquier cambio (incluida la reapertura a "en proceso"),
///     y son los únicos que pueden pasar a "cerrada" (verificación de que de verdad se resolvió) o "cancelada".
///   • "cerrada" solo se alcanza desde "resuelta".
///   • Técnico (solo en sus incidencias asignadas): abierta → en proceso · en proceso → en espera / resuelta · en espera → en proceso.
/// Los mensajes de error están en <see cref="EstadoErrors"/>.
/// </summary>
public static class EstadoIncidenciaValidator
{
    public const string Abierta   = "abierta";
    public const string EnProceso = "en proceso";
    public const string EnEspera  = "en espera";
    public const string Resuelta  = "resuelta";
    public const string Cerrada   = "cerrada";
    public const string Cancelada = "cancelada";

    /// <summary>Cambios que puede hacer un técnico: estado actual → estados a los que puede pasar.</summary>
    private static readonly Dictionary<string, string[]> FlujoTecnico = new()
    {
        [Abierta]   = [EnProceso],
        [EnProceso] = [EnEspera, Resuelta],
        [EnEspera]  = [EnProceso]
    };

    /// <summary>Estados a los que un técnico puede pasar una incidencia (en cualquier momento del flujo).</summary>
    private static readonly string[] DestinosTecnico = [EnProceso, EnEspera, Resuelta];

    /// <summary>Normaliza el estado recibido ("En Proceso" → "en proceso") o lanza error si no existe.</summary>
    public static string ValidarEstado(string? valor)
    {
        var estado = string.IsNullOrWhiteSpace(valor)
            ? null
            : IncidenciaValidator.Estados.FirstOrDefault(e => TextoNormalizer.Iguales(e, valor));

        return estado ?? throw EstadoErrors.NoValido(valor, IncidenciaValidator.Estados);
    }

    /// <summary>Lanza el error correspondiente si el usuario no puede pasar de <paramref name="actual"/> a <paramref name="nuevo"/>.</summary>
    public static void ValidarTransicion(string actual, string nuevo, UsuarioActual usuario)
    {
        if (actual == nuevo)
            throw EstadoErrors.MismoEstado(actual);

        // "cerrada" es la verificación final: solo se alcanza desde "resuelta"
        if (nuevo == Cerrada && actual != Resuelta)
            throw EstadoErrors.CerradaSoloDesdeResuelta(actual);

        if (!usuario.EsRol(RolesSistema.Tecnico))
            return; // Root, Soporte y Administrativos: cualquier otro cambio

        if (!DestinosTecnico.Contains(nuevo))
            throw EstadoErrors.TecnicoSinPermiso(nuevo, DestinosTecnico);

        var permitidos = FlujoTecnico.TryGetValue(actual, out var destinos) ? destinos : [];
        if (!permitidos.Contains(nuevo))
            throw EstadoErrors.TransicionNoPermitida(actual, nuevo, permitidos);
    }
}
