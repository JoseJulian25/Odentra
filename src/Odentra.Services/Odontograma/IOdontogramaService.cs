using Odentra.Data.Entities;
using Odentra.Services.Odontograma.DTOs;

namespace Odentra.Services.Odontograma;

public interface IOdontogramaService
{
    Task<IReadOnlyList<OdontogramaPacienteDto>> GetActivePatientsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OdontogramaPiezaDto>> GetByPatientAsync(int pacienteId, CancellationToken cancellationToken = default);
    Task SaveAsync(int pacienteId, int piezaDentalId, EstadoOdontograma estado, string? observaciones, string userId, CancellationToken cancellationToken = default);
}