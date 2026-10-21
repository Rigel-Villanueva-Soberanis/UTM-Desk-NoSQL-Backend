using Api.Middlewares;
using Api.Security;
using Application.Services;
using Infrastructure.Data;       // MongoDbContext
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ─── Servicios ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<ApiKeyOperationFilter>();

    // ─── JWT Bearer en Swagger UI (el candadito 🔒) ───────────────────────────
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description  = "Pega aquí tu token JWT. Ejemplo: eyJhbGci..."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// AutoMapper v16 — se pasa un tipo marcador del ensamblado que contiene los Profiles
builder.Services.AddAutoMapper(cfg => { }, typeof(Application.Profiles.EntityProfile));

// Infraestructura — MongoDB
var mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"]!;
var mongoDatabaseName     = builder.Configuration["MongoDB:DatabaseName"]!;
builder.Services.AddSingleton(new MongoDbContext(mongoConnectionString, mongoDatabaseName));

// Repositorios e Interfaces
builder.Services.AddScoped<Application.Interfaces.IEntityRepository, EntityRepository>();
builder.Services.AddScoped<Application.Interfaces.ICatalogoRepository, CatalogoRepository>();
builder.Services.AddScoped<Application.Interfaces.IUsuarioRepository, UsuarioRepository>();

// Servicios de aplicación
builder.Services.AddScoped<EntityService>();
builder.Services.AddScoped<CatalogoService>();
builder.Services.AddScoped<AuthService>();

// ─── App ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
