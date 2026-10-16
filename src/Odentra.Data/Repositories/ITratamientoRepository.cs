using Odentra.Data.Entities;

namespace Odentra.Data.Repositories;

public interface ITratamientoRepository
{
    Task<IReadOnlyList<Tratamiento>> SearchAsync(
        string? searchTerm = null,
        EstadoRegistro? estado = null,
        CancellationToken cancellationToken = default);

    Task<Tratamiento?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Tratamiento tratamiento, CancellationToken cancellationToken = default);

    Task UpdateAsync(Tratamiento tratamiento, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}