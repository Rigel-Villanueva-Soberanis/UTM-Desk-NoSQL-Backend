namespace Application.DTOs.Responses;

/// <summary>Respuesta paginada genérica.</summary>
public class PaginadoResponse<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public long Total { get; set; }
    public int TotalPaginas { get; set; }
}
