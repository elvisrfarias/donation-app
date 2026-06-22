using DoacaoApp.Api.Extensions;
using DoacaoApp.Application;
using DoacaoApp.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog: console + arquivo, structured logging com contexto (lê do appsettings).
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Camadas (Clean Architecture).
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Migrations (quando habilitado) + seed de roles/admin.
await app.ApplyMigrationsAndSeedAsync();

// Garante o bucket do object storage (MinIO/S3).
await app.InitializeStorageAsync();

app.UseApiPipeline();

app.Run();

// Exposto para os testes de integração (WebApplicationFactory<Program>).
public partial class Program;