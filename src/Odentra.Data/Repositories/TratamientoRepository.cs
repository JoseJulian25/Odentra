using Microsoft.EntityFrameworkCore;
using Odentra.Data.Entities;

namespace Odentra.Data.Repositories;

public class TratamientoRepository : ITratamientoRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TratamientoRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Tratamiento>> SearchAsync(
        string? searchTerm = null,
        EstadoRegistro? estado = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Tratamiento> query = _dbContext.Tratamientos.AsNoTracking();

        if (estado.HasValue)
        {
            query = query.Where(t => t.Estado == estado.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedTerm = searchTerm.Trim().ToLower();
            query = query.Where(t =>
                t.Nombre.ToLower().Contains(normalizedTerm) ||
                (t.Descripcion != null && t.Descripcion.ToLower().Contains(normalizedTerm)));
        }

        return await query
            .OrderBy(t => t.Nombre)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tratamiento?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Tratamientos
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task AddAsync(Tratamiento tratamiento, CancellationToken cancellationToken = default)
    {
        await _dbContext.Tratamientos.AddAsync(tratamiento, cancellationToken);
    }

    public Task UpdateAsync(Tratamiento tratamiento, CancellationToken cancellationToken = default)
    {
        _dbContext.Tratamientos.Update(tratamiento);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}