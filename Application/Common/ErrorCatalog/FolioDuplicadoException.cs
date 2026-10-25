namespace Application.Common.ErrorCatalog;

/// <summary>
/// La base de datos rechazó la incidencia porque su folio ya existe (índice único ux_folio_incidencia).
/// La lanza el repositorio para que el servicio resincronice el contador y reintente,
/// sin que Application tenga que conocer las excepciones de MongoDB.
/// </summary>
public class FolioDuplicadoException : Exception
{
    public string Folio { get; }

    public FolioDuplicadoException(string folio)
        : base($"El folio '{folio}' ya existe.")
    {
        Folio = folio;
    }
}
