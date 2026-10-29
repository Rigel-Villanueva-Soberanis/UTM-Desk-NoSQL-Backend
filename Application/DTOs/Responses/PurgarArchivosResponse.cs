namespace Application.DTOs.Responses;

public class PurgarArchivosResponse
{
    public int Solicitados { get; set; }
    public int Eliminados { get; set; }
    public int Fallidos { get; set; }
}
