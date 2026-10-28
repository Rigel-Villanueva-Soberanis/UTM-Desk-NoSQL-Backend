using Api.Configuration;
using Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// ─── Servicios (organizados por módulos en Api/Configuration) ────────────────
builder.Services.AddCustomControllers();
builder.Services.AddSwaggerConfiguration();
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddInfrastructureAndApplication(builder.Configuration);

// ─── Pipeline HTTP ──────────────────────────────────────────────────────────
var app = builder.Build();

// Swagger: siempre en desarrollo; en producción solo si se activa con la variable Swagger__Habilitado=true
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Habilitado"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors(CorsConfiguration.Politica);   // antes de la autenticación: el preflight (OPTIONS) no lleva token
// En la nube (Render, etc.) el HTTPS lo termina la plataforma y la API escucha en HTTP dentro del contenedor
if (!string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase))
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
