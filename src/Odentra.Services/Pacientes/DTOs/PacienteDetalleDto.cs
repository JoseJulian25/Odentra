using Odentra.Data.Entities;

namespace Odentra.Services.Pacientes.DTOs;

public sealed record PacienteDetalleDto(
    int Id,
    string Nombres,
    string Apellidos,
    string? Identificacion,
    DateOnly? FechaNacimiento,
    string? Sexo,
    string? Telefono,
    string? Correo,
    string? Direccion,
    string? ContactoEmergenciaNombre,
    string? ContactoEmergenciaTelefono,
    string? ContactoEmergenciaRelacion,
    DateTime FechaRegistro,
    EstadoRegistro Estado);