using Application.Interfaces;
using Application.Services;
using Application.Validators.Catalogos;
using Application.Validators.Incidencias;
using Infrastructure.Data;
using Infrastructure.Repositories;

namespace Api.Configuration;

/// <summary>
/// Registro centralizado de dependencias (MongoDB, Repositorios, Servicios y AutoMapper).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureAndApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // AutoMapper — registra todos los Profiles del ensamblado Application
        services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Application.Profiles.EntityProfile).Assembly));
        // Acción de mapeo que necesita servicios (pone el enlace temporal firmado a las evidencias)
        services.AddTransient<Application.Profiles.EnlaceTemporalEvidencia>();

        // Infraestructura — MongoDB
        var mongoConnectionString = configuration["MongoDB:ConnectionString"]!;
        var mongoDatabaseName     = configuration["MongoDB:DatabaseName"]!;
        services.AddSingleton(new MongoDbContext(mongoConnectionString, mongoDatabaseName));

        // Infraestructura — Redis en Docker (folios atómicos). Se conecta hasta el primer uso.
        // appsettings.json → "ConnectionStrings": { "Redis": "localhost:6379" }
        services.AddSingleton(new RedisContext(configuration.GetConnectionString("Redis")));

        // Infraestructura — Cloudinary (archivos de evidencias). Se crea hasta el primer uso.
        services.AddSingleton(_ => new CloudinaryContext(
            configuration["CloudinarySettings:CloudName"],
            configuration["CloudinarySettings:ApiKey"],
            configuration["CloudinarySettings:ApiSecret"]));

        // Repositorios e Interfaces
        services.AddScoped<IEntityRepository, EntityRepository>();
        services.AddScoped<ICatalogoRepository, CatalogoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IIncidenciaRepository, IncidenciaRepository>();
        services.AddScoped<IFolioRepository, FolioRepository>();
        services.AddScoped<IArchivoRepository, ArchivoRepository>();
        services.AddScoped<IAnaliticaRepository, AnaliticaRepository>();

        // Validadores de catálogos (uno por catálogo editable: permisos + estructura)
        services.AddSingleton<ICatalogoValidator, RolesSistemaValidator>();
        services.AddSingleton<ICatalogoValidator, EdificiosValidator>();
        services.AddSingleton<ICatalogoValidator, CategoriasIncidenciaValidator>();

        // Validadores de incidencias (consultan los catálogos)
        services.AddScoped<IncidenciaValidator>();

        // Servicios de Aplicación
        services.AddScoped<EntityService>();
        services.AddScoped<CatalogoService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UsuarioService>();
        services.AddScoped<FolioService>();
        services.AddScoped<IncidenciaService>();
        services.AddScoped<GestionIncidenciaService>();
        services.AddScoped<AdministracionIncidenciaService>();
        services.AddScoped<AnaliticaService>();
        services.AddScoped<EvidenciaService>();

        return services;
    }
}
