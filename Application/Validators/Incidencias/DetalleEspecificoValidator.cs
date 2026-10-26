using Application.Common.ErrorCatalog;
using Application.Common.ErrorsIncidencias;
using Application.Common.Utilidades;
using MongoDB.Bson;
using System.Globalization;

namespace Application.Validators.Incidencias;

/// <summary>
/// Valida el <c>detalle_especifico</c> de una incidencia contra los <c>campos_dinamicos</c>
/// definidos para su categoría en el catálogo <c>categorias_incidencia</c>.
///   • Solo se aceptan los campos definidos para la categoría.
///   • "requerido": true → el campo es obligatorio.
///   • tipo_input: text / textarea → texto · number → número · select → una de las opciones · date → fecha.
/// Devuelve el detalle limpio (valores normalizados) y agrega a la lista de errores lo que no cumpla.
/// </summary>
public static class DetalleEspecificoValidator
{
    private const string Raiz = "detalleEspecifico";

    public static BsonDocument Validar(BsonValue detalle, BsonArray camposDinamicos, List<ErrorDetalle> errores)
    {
        var resultado = new BsonDocument();

        BsonDocument enviado;
        if (detalle.IsBsonNull)
            enviado = new BsonDocument();
        else if (detalle.IsBsonDocument)
            enviado = detalle.AsBsonDocument;
        else
        {
            errores.Add(DetalleEspecificoErrors.NoEsObjeto(Raiz));
            return resultado;
        }

        var definiciones = camposDinamicos
            .OfType<BsonDocument>()
            .Where(d => d.Contains("clave") && d["clave"].IsString)
            .ToList();
        var clavesDefinidas = definiciones.Select(d => d["clave"].AsString).ToList();

        // Campos que no pertenecen a la categoría
        foreach (var nombre in enviado.Names)
        {
            if (clavesDefinidas.Contains(nombre)) continue;
            errores.Add(clavesDefinidas.Count == 0
                ? DetalleEspecificoErrors.SinCamposAdicionales($"{Raiz}.{nombre}")
                : DetalleEspecificoErrors.CampoNoDefinido($"{Raiz}.{nombre}", clavesDefinidas));
        }

        // Campos definidos
        foreach (var definicion in definiciones)
        {
            var clave     = definicion["clave"].AsString;
            var ruta      = $"{Raiz}.{clave}";
            var tipo      = TextoNormalizer.Normalizar(Texto(definicion, "tipo_input") ?? "text");
            var requerido = definicion.Contains("requerido") && definicion["requerido"].IsBoolean && definicion["requerido"].AsBoolean;

            if (!enviado.Contains(clave) || EstaVacio(enviado[clave]))
            {
                if (requerido) errores.Add(DetalleEspecificoErrors.Obligatorio(ruta));
                continue;
            }

            var valor  = enviado[clave];
            var limpio = tipo switch
            {
                "number" => LeerNumero(valor, ruta, errores),
                "select" => LeerOpcion(valor, definicion, ruta, errores),
                "date"   => LeerFecha(valor, ruta, errores),
                "textarea" => LeerTexto(valor, ruta, errores, 2000),
                _        => LeerTexto(valor, ruta, errores, 500)
            };

            if (limpio is not null)
                resultado[clave] = limpio;
        }

        return resultado;
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static bool EstaVacio(BsonValue valor)
        => valor.IsBsonNull || (valor.IsString && string.IsNullOrWhiteSpace(valor.AsString));

    private static string? Texto(BsonDocument doc, string campo)
        => doc.Contains(campo) && doc[campo].IsString ? doc[campo].AsString : null;

    private static BsonValue? LeerTexto(BsonValue valor, string ruta, List<ErrorDetalle> errores, int maxLongitud)
    {
        if (!valor.IsString)
        {
            errores.Add(DetalleEspecificoErrors.DebeSerTexto(ruta));
            return null;
        }
        var texto = TextoSeguro.Limpiar(valor.AsString);
        if (texto.Length > maxLongitud)
        {
            errores.Add(DetalleEspecificoErrors.MuyLargo(ruta, maxLongitud));
            return null;
        }
        return new BsonString(texto);
    }

    /// <summary>Acepta número JSON o texto numérico ("25"). Se guarda como número.</summary>
    private static BsonValue? LeerNumero(BsonValue valor, string ruta, List<ErrorDetalle> errores)
    {
        if (valor.IsNumeric) return valor;

        if (valor.IsString && double.TryParse(valor.AsString.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var numero))
            return new BsonDouble(numero);

        errores.Add(DetalleEspecificoErrors.DebeSerNumero(ruta));
        return null;
    }

    /// <summary>El valor debe ser una de las opciones (ignora mayúsculas y acentos); se guarda tal como está en el catálogo.</summary>
    private static BsonValue? LeerOpcion(BsonValue valor, BsonDocument definicion, string ruta, List<ErrorDetalle> errores)
    {
        var opciones = definicion.Contains("opciones") && definicion["opciones"].IsBsonArray
            ? definicion["opciones"].AsBsonArray.Where(o => o.IsString).Select(o => o.AsString).ToList()
            : new List<string>();

        var elegida = valor.IsString
            ? opciones.FirstOrDefault(o => TextoNormalizer.Iguales(o, valor.AsString))
            : null;

        if (elegida is null)
        {
            errores.Add(DetalleEspecificoErrors.OpcionNoValida(ruta, opciones));
            return null;
        }
        return new BsonString(elegida);
    }

    /// <summary>Acepta fecha en texto (ej. "2026-10-24"). Se guarda como BSON Date.</summary>
    private static BsonValue? LeerFecha(BsonValue valor, string ruta, List<ErrorDetalle> errores)
    {
        if (valor.IsString && DateTime.TryParse(valor.AsString.Trim(), CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var fecha))
            return new BsonDateTime(fecha);

        errores.Add(DetalleEspecificoErrors.FechaInvalida(ruta));
        return null;
    }
}
