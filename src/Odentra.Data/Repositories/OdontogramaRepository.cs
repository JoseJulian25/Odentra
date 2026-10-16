using Microsoft.EntityFrameworkCore;
using Odentra.Data.Entities;

namespace Odentra.Data.Repositories;

public sealed class OdontogramaRepository(ApplicationDbContext dbContext) : IOdontogramaRepository
{
    public Task<IReadOnlyList<Paciente>> GetActivePatientsAsync(CancellationToken cancellationToken = default) =>
        dbContext.Pacientes.AsNoTracking()
            .Where(patient => patient.Estado == EstadoRegistro.Activo)
            .OrderBy(patient => patient.Apellidos)
            .ThenBy(patient => patient.Nombres)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<Paciente>)task.Result, cancellationToken);

    public Task<IReadOnlyList<PiezaDental>> GetPiecesAsync(CancellationToken cancellationToken = default) =>
        dbContext.PiezasDentales.AsNoTracking()
            .OrderBy(piece => piece.NumeroFDI)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<PiezaDental>)task.Result, cancellationToken);

    public Task<IReadOnlyList<RegistroOdontograma>> GetRecordsAsync(int pacienteId, CancellationToken cancellationToken = default) =>
        dbContext.RegistrosOdontograma.AsNoTracking()
            .Include(record => record.PiezaDental)
            .Include(record => record.Odontologo)
            .Where(record => record.PacienteId == pacienteId)
            .ToListAsync(cancellationToken)
            .ContinueWith(task => (IReadOnlyList<RegistroOdontograma>)task.Result, cancellationToken);

    public Task<int?> GetOdontologoIdByUserAsync(string userId, CancellationToken cancellationToken = default) =>
        dbContext.Odontologos.AsNoTracking()
            .Where(dentist => dentist.UsuarioId == userId)
            .Select(dentist => (int?)dentist.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<RegistroOdontograma?> GetRecordAsync(int pacienteId, int piezaDentalId, CancellationToken cancellationToken = default) =>
        dbContext.RegistrosOdontograma
            .FirstOrDefaultAsync(record => record.PacienteId == pacienteId && record.PiezaDentalId == piezaDentalId, cancellationToken);

    public async Task AddAsync(RegistroOdontograma record, CancellationToken cancellationToken = default) =>
        await dbContext.RegistrosOdontograma.AddAsync(record, cancellationToken);

    public async Task SaveChangesWithAuditAsync(RegistroAuditoria audit, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        dbContext.RegistrosAuditoria.Add(audit);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}