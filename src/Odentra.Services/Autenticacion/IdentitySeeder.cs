using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Odentra.Data.Entities;

namespace Odentra.Services.Autenticacion;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<Rol>>();
        var userManager = services.GetRequiredService<UserManager<Usuario>>();

        foreach (var roleName in new[] { "Administrador", "Recepcionista", "Odontólogo" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new Rol { Name = roleName, Estado = EstadoRegistro.Activo });
                EnsureSucceeded(result, $"crear el rol {roleName}");
            }
        }

        var email = configuration["Authentication:AdminEmail"] ?? "admin@odentra.com";
        var password = configuration["Authentication:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Authentication:AdminPassword no esta configurada; no se creo el administrador inicial.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new Usuario { UserName = email, Email = email, Nombre = "Administrador", Estado = EstadoRegistro.Activo, EmailConfirmed = true };
            var result = await userManager.CreateAsync(admin, password);
            EnsureSucceeded(result, "crear el administrador inicial");
        }

        if (!await userManager.IsInRoleAsync(admin, "Administrador"))
        {
            var result = await userManager.AddToRoleAsync(admin, "Administrador");
            EnsureSucceeded(result, "asignar el rol Administrador");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"No se pudo {operation}: {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }
}