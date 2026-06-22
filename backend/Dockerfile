# ============================================================================
#  Build stage — SDK completo, restaura e publica a API.
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia primeiro os arquivos de projeto/props para aproveitar o cache de restore.
COPY ["global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/DoacaoApp.Domain/DoacaoApp.Domain.csproj", "src/DoacaoApp.Domain/"]
COPY ["src/DoacaoApp.Application/DoacaoApp.Application.csproj", "src/DoacaoApp.Application/"]
COPY ["src/DoacaoApp.Infrastructure/DoacaoApp.Infrastructure.csproj", "src/DoacaoApp.Infrastructure/"]
COPY ["src/DoacaoApp.Api/DoacaoApp.Api.csproj", "src/DoacaoApp.Api/"]

RUN dotnet restore "src/DoacaoApp.Api/DoacaoApp.Api.csproj"

# Copia o restante do código e publica.
COPY src/ src/
RUN dotnet publish "src/DoacaoApp.Api/DoacaoApp.Api.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

# ============================================================================
#  Runtime stage — imagem alpine enxuta, usuário não-root.
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# ICU para globalização (normalização de strings, CPF, etc.) — alpine não traz por padrão.
RUN apk add --no-cache icu-libs icu-data-full
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

RUN adduser -D -u 1000 appuser
COPY --from=build /app/publish .
RUN mkdir -p /app/logs && chown -R appuser:appuser /app
USER appuser

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=5 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "DoacaoApp.Api.dll"]