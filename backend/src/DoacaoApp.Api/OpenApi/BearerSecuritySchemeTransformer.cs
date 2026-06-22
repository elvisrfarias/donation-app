using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace DoacaoApp.Api.OpenApi;

/// <summary>
/// Adiciona o esquema de segurança Bearer (JWT) ao documento OpenAPI para que o Scalar
/// exiba o botão "Authorize" e permita testar endpoints protegidos.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Informe o token JWT. Exemplo: \"Bearer {token}\".",
            Reference = new OpenApiReference
            {
                Id = "Bearer",
                Type = ReferenceType.SecurityScheme,
            },
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["Bearer"] = scheme;

        document.SecurityRequirements.Add(new OpenApiSecurityRequirement
        {
            [scheme] = [],
        });

        return Task.CompletedTask;
    }
}