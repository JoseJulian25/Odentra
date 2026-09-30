using Odentra.Data.Entities;

namespace Odentra.Services.Usuarios.DTOs;

public sealed record UsuarioListItemDto(
    string Id,
    string Nombre,
    string Email,
    EstadoRegistro Estado,
    DateTime FechaCreacion,
    IReadOnlyList<string> Roles);