using Odentra.Data.Entities;
using Odentra.Services.Odontograma.DTOs;

namespace Odentra.Services.Odontograma;

public interface IOdontogramaService
{
    /// <summary>
    /// Pacientes activos para el panel de búsqueda, ordenados alfabéticamente por
    /// nombre completo. Si se indica <paramref name="searchTerm"/> se filtran por
    /// nombres o apellidos.
    /// </summary>
    Task<IReadOnlyList<OdontogramaPacienteDto>> GetActivePatientsAsync(
        string? searchTerm = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OdontogramaPiezaDto>> GetByPatientAsync(int pacienteId, CancellationToken cancellationToken = default);
    Task SaveAsync(int pacienteId, int piezaDentalId, EstadoOdontograma estado, string? observaciones, string userId, CancellationToken cancellationToken = default);
}