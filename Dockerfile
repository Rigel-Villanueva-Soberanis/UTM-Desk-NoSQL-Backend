# ─────────────────────────────────────────────────────────────────────────────
# UTM Desk NoSQL — Imagen de la API (.NET 10) para desplegar (Render, Railway, Fly.io...)
#
# Construir local:  docker build -t utmdesk-api .
# Probar local:     docker run -p 8080:8080 --env-file .env utmdesk-api
#
# Los secretos NO van en la imagen: se pasan como variables de entorno (ver Guia de Despliegue).
# ─────────────────────────────────────────────────────────────────────────────

# ── Etapa 1: compilar ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los .csproj: así Docker reutiliza el restore si el código cambia pero los paquetes no
COPY Domain/Domain.csproj Domain/
COPY Application/Application.csproj Application/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY Api/Api.csproj Api/
RUN dotnet restore Api/Api.csproj

COPY Domain/ Domain/
COPY Application/ Application/
COPY Infrastructure/ Infrastructure/
COPY Api/ Api/
RUN dotnet publish Api/Api.csproj -c Release -o /app/publish --no-restore

# ── Etapa 2: imagen final (solo el runtime, más ligera) ──────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Render/Railway mandan el puerto en la variable PORT; si no existe usa 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet Api.dll"]
