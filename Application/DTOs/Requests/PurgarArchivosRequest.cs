namespace Application.DTOs.Requests;

/// <summary>Public ids de Cloudinary a eliminar (lo envía el Trigger de Atlas).</summary>
public class PurgarArchivosRequest
{
    public List<string> PublicIds { get; set; } = new();
}
