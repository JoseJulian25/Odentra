using Odentra.Data.Entities;

namespace Odentra.Services.Citas.DTOs;

public sealed record CitaListItemDto(
    int Id,
    string PacienteNombre,
    string OdontologoNombre,
    DateOnly Fecha,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    string? Motivo,
    EstadoCita Estado);
