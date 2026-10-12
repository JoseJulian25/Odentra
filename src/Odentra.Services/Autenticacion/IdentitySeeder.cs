using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Odentra.Data;
using Odentra.Data.Entities;
using Odentra.Services.Autorizacion;

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

        var permissions = new Dictionary<string, Permiso>();
        foreach (var definition in PermissionCatalog.All)
        {
            var permission = await services.GetRequiredService<ApplicationDbContext>().Permisos
                .SingleOrDefaultAsync(item => item.Nombre == definition.Nombre);
            if (permission is null)
            {
                permission = new Permiso
                {
                    Nombre = definition.Nombre,
                    Modulo = definition.Modulo,
                    Descripcion = definition.Descripcion
                };
                services.GetRequiredService<ApplicationDbContext>().Permisos.Add(permission);
            }

            permissions[permission.Nombre] = permission;
        }

        await services.GetRequiredService<ApplicationDbContext>().SaveChangesAsync();

        foreach (var rolePermissions in PermissionCatalog.DefaultRolePermissions)
        {
            var role = await roleManager.FindByNameAsync(rolePermissions.Key);
            if (role is null) continue;

            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var assignedPermissionIds = await dbContext.RolesPermisos
                .Where(item => item.RolId == role.Id)
                .Select(item => item.PermisoId)
                .ToHashSetAsync();

            var missingPermissions = rolePermissions.Value
                .Select(permission => permissions[permission])
                .Where(permission => !assignedPermissionIds.Contains(permission.Id))
                .Select(permission => new RolPermiso
                {
                    RolId = role.Id,
                    PermisoId = permission.Id
                })
                .ToList();

            dbContext.RolesPermisos.AddRange(missingPermissions);
            if (missingPermissions.Count == 0) continue;

            await dbContext.SaveChangesAsync();
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