using System.Text;

namespace Application.Common.Utilidades;

/// <summary>
/// Limpieza de texto libre (título, descripción, notas, motivos...) antes de guardarlo.
///
/// Los símbolos como $, {, }, comillas o &lt; &gt; SE CONSERVAN: son texto normal y MongoDB nunca los
/// interpreta como operadores cuando viajan como valor (el driver los manda como string tipado).
/// Lo que sí se limpia es lo que no es texto escribible:
///   • caracteres de control y el carácter nulo (\0), salvo salto de línea y tabulador;
///   • saltos de línea de Windows (\r\n) → \n;
///   • caracteres invisibles de formato (ancho cero, marca de orden de bytes, direccionales),
///     que sirven para esconder texto o falsificar lo que se lee;
///   • Unicode sin normalizar → se guarda en forma compuesta (FormC), para que "é" siempre sea igual.
/// El escape de HTML NO se hace aquí: eso se hace al mostrarlo (Vue ya escapa con {{ }}; nunca usar v-html).
/// </summary>
public static class TextoSeguro
{
    public static string Limpiar(string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return string.Empty;

        var texto = valor.Normalize(NormalizationForm.FormC).Replace("\r\n", "\n").Replace('\r', '\n');
        var sb    = new StringBuilder(texto.Length);

        foreach (var c in texto)
        {
            if (c is '\n' or '\t') { sb.Append(c); continue; }
            if (char.IsControl(c)) continue;                                       // \0, \u0007, \u001b...
            if (c is '​' or '‌' or '‍' or '⁠' or '﻿') continue; // ancho cero / BOM
            if (c is >= '‪' and <= '‮' or >= '⁦' and <= '⁩') continue; // direccionales
            sb.Append(c);
        }

        return sb.ToString().Trim();
    }
}
