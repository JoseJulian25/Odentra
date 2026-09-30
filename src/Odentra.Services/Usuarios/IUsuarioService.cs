using Odentra.Data.Entities;
using Odentra.Services.Usuarios.DTOs;

namespace Odentra.Services.Usuarios;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioListItemDto>> SearchAsync(string? searchTerm, EstadoRegistro? estado, CancellationToken cancellationToken = default);
    Task<UsuarioFormDto?> GetFormAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAvailableRolesAsync(CancellationToken cancellationToken = default);
    Task CreateAsync(UsuarioFormDto model, CancellationToken cancellationToken = default);
    Task UpdateAsync(UsuarioFormDto model, CancellationToken cancellationToken = default);
    Task SetStatusAsync(string id, EstadoRegistro estado, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(string id, string password, CancellationToken cancellationToken = default);
}