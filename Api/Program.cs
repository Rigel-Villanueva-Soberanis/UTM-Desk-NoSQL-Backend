using Api.Middlewares;
using Api.Security;
using Application.Services;
using Infrastructure.Data;
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ─── Servicios ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<ApiKeyOperationFilter>();
});

// AutoMapper v16 — se pasa un tipo marcador del ensamblado que contiene los Profiles
builder.Services.AddAutoMapper(cfg => { }, typeof(Application.Profiles.EntityProfile));

// Infraestructura
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddSingleton(new DbConnectionFactory(connectionString));

// Repositorios e Interfaces
builder.Services.AddScoped<Application.Interfaces.IEntityRepository, EntityRepository>();

// Servicios de aplicación
builder.Services.AddScoped<EntityService>();

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
