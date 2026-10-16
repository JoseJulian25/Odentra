using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Odentra.Data;
using Odentra.Data.Entities;
using Odentra.Services.Odontologos.DTOs;
using Odentra.Services.Usuarios.DTOs;

namespace Odentra.Services.Usuarios;

public sealed class UsuarioService(
    UserManager<Usuario> userManager,
    RoleManager<Rol> roleManager,
    ApplicationDbContext dbContext) : IUsuarioService
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
            Roles = (await userManager.GetRolesAsync(user)).ToList(),
            PerfilOdontologo = await GetOdontologoFormAsync(user.Id, cancellationToken)
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
        ValidateOdontologoProfile(model);
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

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await userManager.CreateAsync(user, model.Password ?? string.Empty);
            EnsureSucceeded(result);
            await ReplaceRolesAsync(user, model.Roles, cancellationToken);
            await SaveOdontologoProfileAsync(user.Id, model, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateAsync(UsuarioFormDto model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Id)) throw new InvalidOperationException("El usuario no existe.");

        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null) throw new InvalidOperationException("El usuario no existe.");
        ValidateOdontologoProfile(model);

        var email = model.Email.Trim();
        user.Nombre = model.Nombre.Trim();
        user.Estado = model.Estado;
        user.Email = email;
        user.UserName = email;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await userManager.UpdateAsync(user);
            EnsureSucceeded(result);
            await ReplaceRolesAsync(user, model.Roles, cancellationToken);
            await SaveOdontologoProfileAsync(user.Id, model, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                EnsureSucceeded(await userManager.ResetPasswordAsync(user, token, model.Password));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
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

    private async Task<OdontologoFormDto?> GetOdontologoFormAsync(string userId, CancellationToken cancellationToken)
    {
        var odontologo = await dbContext.Odontologos.AsNoTracking().SingleOrDefaultAsync(item => item.UsuarioId == userId, cancellationToken);
        return odontologo is null ? null : new OdontologoFormDto
        {
            Id = odontologo.Id,
            Estado = odontologo.Estado,
            Nombres = odontologo.Nombres,
            Apellidos = odontologo.Apellidos,
            NumeroLicencia = odontologo.NumeroLicencia ?? string.Empty,
            Especialidad = odontologo.Especialidad,
            Telefono = odontologo.Telefono,
            Correo = odontologo.Correo
        };
    }

    private async Task SaveOdontologoProfileAsync(string userId, UsuarioFormDto model, CancellationToken cancellationToken)
    {
        var isDentist = model.Roles.Contains("Odontólogo", StringComparer.Ordinal);
        var existing = await dbContext.Odontologos.SingleOrDefaultAsync(item => item.UsuarioId == userId, cancellationToken);

        if (!isDentist)
        {
            if (existing is not null) existing.UsuarioId = null;
            return;
        }

        var profile = model.PerfilOdontologo!;
        var licenseInUse = await dbContext.Odontologos.AnyAsync(item => item.NumeroLicencia == profile.NumeroLicencia && item.UsuarioId != userId, cancellationToken);
        if (licenseInUse) throw new InvalidOperationException("El número de licencia ya está asociado a otro odontólogo.");

        if (existing is null)
        {
            existing = new Odontologo { UsuarioId = userId };
            dbContext.Odontologos.Add(existing);
        }

        existing.Nombres = profile.Nombres.Trim();
        existing.Apellidos = profile.Apellidos.Trim();
        existing.NumeroLicencia = profile.NumeroLicencia.Trim();
        existing.Especialidad = Normalize(profile.Especialidad);
        existing.Telefono = Normalize(profile.Telefono);
        existing.Correo = Normalize(profile.Correo);
        existing.Estado = profile.Estado;
    }

    private static void ValidateOdontologoProfile(UsuarioFormDto model)
    {
        if (model.Roles.Contains("Odontólogo", StringComparer.Ordinal) && model.PerfilOdontologo is null)
        {
            throw new InvalidOperationException("Completa el perfil profesional del odontólogo.");
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}