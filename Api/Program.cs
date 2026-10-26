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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors(CorsConfiguration.Politica);   // antes de la autenticación: el preflight (OPTIONS) no lleva token
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
