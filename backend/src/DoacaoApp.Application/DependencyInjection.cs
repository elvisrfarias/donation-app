using DoacaoApp.Application.Common.Behaviors;
using DoacaoApp.Application.Common.Security;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DoacaoApp.Application;

/// <summary>Composition root da camada Application (MediatR + FluentValidation).</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(CollectionPointScopeBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}