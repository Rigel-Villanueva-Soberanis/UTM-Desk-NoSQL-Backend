namespace Application.Common;

/// <summary>
/// Protección contra inyección NoSQL en MongoDB.
///
/// ¿POR QUÉ EL REPOSITORIO YA ES SEGURO?
/// ────────────────────────────────────────
/// El MongoDB Driver con LINQ genera consultas parametrizadas:
///   _collection.Find(u => u.Username == username)
///   → genera: { "usuario": "valor_literal" }
/// El valor nunca se interpreta como operador, sin importar lo que contenga.
/// Esto equivale a los "prepared statements" de SQL.
///
/// ¿DÓNDE SÍ HAY RIESGO?
/// ───────────────────────
/// 1. Cuando se construyen filtros con strings crudos: FilterDefinition.Parse("...")
/// 2. Cuando se usa BsonDocument.Parse() con input del usuario
/// 3. Cuando campos de texto se usan directamente en índices o $regex sin sanitizar
///
/// ESTE SANITIZADOR protege la capa de Servicio antes de que los datos
/// lleguen al repositorio, como segunda línea de defensa.
/// </summary>
public static class NoSqlSanitizer
{
    /// <summary>
    /// Caracteres y prefijos que MongoDB interpreta como operadores de consulta.
    /// Si un campo de texto libre los contiene, se rechaza la petición.
    /// </summary>
    private static readonly char[] OperatorChars = ['$', '{', '}'];

    /// <summary>
    /// Valida que un campo de texto no contenga operadores NoSQL.
    /// Lanza <see cref="DomainException"/> si detecta un posible ataque de inyección.
    /// </summary>
    /// <param name="valor">Valor del campo a validar.</param>
    /// <param name="campo">Nombre del campo (para el mensaje de error).</param>
    public static void Validar(string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor)) return;

        if (valor.IndexOfAny(OperatorChars) >= 0)
        {
            throw new DomainException(
                ApiErrors.ValidationFailed,
                $"El campo '{campo}' contiene caracteres no permitidos.");
        }
    }

    /// <summary>
    /// Valida múltiples campos de texto en una sola llamada.
    /// </summary>
    /// <param name="campos">Pares (valor, nombre del campo).</param>
    public static void ValidarTodos(params (string? valor, string campo)[] campos)
    {
        foreach (var (valor, campo) in campos)
            Validar(valor, campo);
    }

    /// <summary>
    /// Sanitiza un string eliminando caracteres peligrosos en lugar de rechazarlo.
    /// Úsalo para campos donde prefieres limpiar silenciosamente (logs, comentarios).
    /// Para campos de identidad (correo, usuario) usa <see cref="Validar"/> en su lugar.
    /// </summary>
    public static string Limpiar(string valor)
        => valor.Replace("$", "").Replace("{", "").Replace("}", "").Trim();
}
