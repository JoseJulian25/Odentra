using Odentra.Data.Entities;

namespace Odentra.Services.Odontologos.DTOs;

public sealed record OdontologoListItemDto(
    int Id,
    string NombreCompleto,
    string? NumeroLicencia,
    string? Especialidad,
    string? Telefono,
    string? Correo,
    EstadoRegistro Estado);
