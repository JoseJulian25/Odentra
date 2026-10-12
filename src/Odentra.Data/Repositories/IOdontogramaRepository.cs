using Odentra.Data.Entities;

namespace Odentra.Data.Repositories;

public interface IOdontogramaRepository
{
    Task<IReadOnlyList<Paciente>> GetActivePatientsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PiezaDental>> GetPiecesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RegistroOdontograma>> GetRecordsAsync(int pacienteId, CancellationToken cancellationToken = default);
    Task<int?> GetOdontologoIdByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<RegistroOdontograma?> GetRecordAsync(int pacienteId, int piezaDentalId, CancellationToken cancellationToken = default);
    Task AddAsync(RegistroOdontograma record, CancellationToken cancellationToken = default);
    Task SaveChangesWithAuditAsync(RegistroAuditoria audit, CancellationToken cancellationToken = default);
}