using Application.Interfaces;
using Application.Services;
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

        // Infraestructura — MongoDB
        var mongoConnectionString = configuration["MongoDB:ConnectionString"]!;
        var mongoDatabaseName     = configuration["MongoDB:DatabaseName"]!;
        services.AddSingleton(new MongoDbContext(mongoConnectionString, mongoDatabaseName));

        // Repositorios e Interfaces
        services.AddScoped<IEntityRepository, EntityRepository>();
        services.AddScoped<ICatalogoRepository, CatalogoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        // Servicios de Aplicación
        services.AddScoped<EntityService>();
        services.AddScoped<CatalogoService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UsuarioService>();

        return services;
    }
}
