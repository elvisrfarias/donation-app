using DoacaoApp.Application.Authentication;
using DoacaoApp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoacaoApp.Infrastructure.Persistence.Seed;

/// <summary>Garante as roles do sistema e um usuário Admin inicial (quando habilitado).</summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var seedOptions = services.GetRequiredService<IOptions<SeedOptions>>().Value;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");

        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                logger.LogInformation("Role criada: {Role}", role);
            }
        }

        if (!seedOptions.Enabled)
        {
            return;
        }

        if (await userManager.FindByEmailAsync(seedOptions.AdminEmail) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = seedOptions.AdminEmail,
            Email = seedOptions.AdminEmail,
            FullName = "Administrador",
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(admin, seedOptions.AdminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
            logger.LogInformation("Usuário admin de seed criado: {Email}", seedOptions.AdminEmail);
        }
        else
        {
            logger.LogWarning(
                "Falha ao criar admin de seed: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}