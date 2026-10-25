using CloudinaryDotNet;

namespace Infrastructure.Data;

/// <summary>
/// Cliente de Cloudinary (Singleton) con la configuración de appsettings.json → "CloudinarySettings".
/// </summary>
public class CloudinaryContext
{
    public Cloudinary Cliente { get; }
    public string CloudName { get; }

    public CloudinaryContext(string? cloudName, string? apiKey, string? apiSecret)
    {
        if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            throw new InvalidOperationException("Falta configurar 'CloudinarySettings' (CloudName, ApiKey, ApiSecret) en appsettings.json.");

        CloudName = cloudName;
        Cliente   = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        Cliente.Api.Secure = true;   // siempre https
    }
}
