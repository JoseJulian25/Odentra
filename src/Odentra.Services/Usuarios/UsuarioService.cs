using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Odentra.Data.Entities;
using Odentra.Services.Usuarios.DTOs;

namespace Odentra.Services.Usuarios;

public sealed class UsuarioService(
    UserManager<Usuario> userManager,
    RoleManager<Rol> roleManager) : IUsuarioService
{
    public async Task<IReadOnlyList<UsuarioListItemDto>> SearchAsync(
        string? searchTerm,
        EstadoRegistro? estado,
        CancellationToken cancellationToken = default)
    {
        var query = userManager.Users.AsNoTracking();

        if (estado.HasValue)
        {
            query = query.Where(user => user.Estado == estado.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(user => user.Nombre.Contains(term) || (user.Email != null && user.Email.Contains(term)));
        }

        var users = await query.OrderBy(user => user.Nombre).ToListAsync(cancellationToken);
        var result = new List<UsuarioListItemDto>(users.Count);

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(new UsuarioListItemDto(user.Id, user.Nombre, user.Email ?? string.Empty, user.Estado, user.FechaCreacion, roles.ToList()));
        }

        return result;
    }

    public async Task<UsuarioFormDto?> GetFormAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null) return null;

        return new UsuarioFormDto
        {
            Id = user.Id,
            Nombre = user.Nombre,
            Email = user.Email ?? string.Empty,
            Estado = user.Estado,
            Roles = (await userManager.GetRolesAsync(user)).ToList()
        };
    }

    public async Task<IReadOnlyList<string>> GetAvailableRolesAsync(CancellationToken cancellationToken = default) =>
        await roleManager.Roles
            .Where(role => role.Estado == EstadoRegistro.Activo)
            .OrderBy(role => role.Name)
            .Select(role => role.Name!)
            .ToListAsync(cancellationToken);

    public async Task CreateAsync(UsuarioFormDto model, CancellationToken cancellationToken = default)
    {
        var email = model.Email.Trim();
        var user = new Usuario
        {
            UserName = email,
            Email = email,
            Nombre = model.Nombre.Trim(),
            Estado = EstadoRegistro.Activo,
            FechaCreacion = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, model.Password ?? string.Empty);
        EnsureSucceeded(result);
        await ReplaceRolesAsync(user, model.Roles, cancellationToken);
    }

    public async Task UpdateAsync(UsuarioFormDto model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Id)) throw new InvalidOperationException("El usuario no existe.");

        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null) throw new InvalidOperationException("El usuario no existe.");

        var email = model.Email.Trim();
        user.Nombre = model.Nombre.Trim();
        user.Estado = model.Estado;
        user.Email = email;
        user.UserName = email;

        var result = await userManager.UpdateAsync(user);
        EnsureSucceeded(result);
        await ReplaceRolesAsync(user, model.Roles, cancellationToken);

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            await ResetPasswordAsync(user.Id, model.Password, cancellationToken);
        }
    }

    public async Task SetStatusAsync(string id, EstadoRegistro estado, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id) ?? throw new InvalidOperationException("El usuario no existe.");
        user.Estado = estado;
        EnsureSucceeded(await userManager.UpdateAsync(user));
    }

    public async Task ResetPasswordAsync(string id, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(id) ?? throw new InvalidOperationException("El usuario no existe.");
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        EnsureSucceeded(await userManager.ResetPasswordAsync(user, token, password));
    }

    private async Task ReplaceRolesAsync(Usuario user, IEnumerable<string> roleNames, CancellationToken cancellationToken)
    {
        var requestedRoles = roleNames.Where(role => !string.IsNullOrWhiteSpace(role)).Distinct().ToArray();
        foreach (var role in requestedRoles)
        {
            if (!await roleManager.RoleExistsAsync(role)) throw new InvalidOperationException($"El rol '{role}' no existe.");
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        EnsureSucceeded(await userManager.RemoveFromRolesAsync(user, currentRoles));
        if (requestedRoles.Length > 0) EnsureSucceeded(await userManager.AddToRolesAsync(user, requestedRoles));
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}