using Odentra.Data.Entities;

namespace Odentra.Services.Pacientes.DTOs;

public sealed record PacienteListItemDto(
    int Id,
    string NombreCompleto,
    string? Identificacion,
    string? Telefono,
    string? Correo,
    DateTime FechaRegistro,
    EstadoRegistro Estado);