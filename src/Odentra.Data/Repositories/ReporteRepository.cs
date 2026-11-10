using Microsoft.EntityFrameworkCore;
using Odentra.Data.Entities;

namespace Odentra.Data.Repositories;

/// <summary>
/// Capa de datos del módulo de Reportes. Todas las consultas son de solo lectura
/// (AsNoTracking) y aplican los filtros en SQL antes de traer los datos.
/// </summary>
public sealed class ReporteRepository(ApplicationDbContext dbContext) : IReporteRepository
{
    public async Task<IReadOnlyList<Cita>> GetCitasAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? odontologoId,
        EstadoCita? estado,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Cita> query = dbContext.Citas
            .AsNoTracking()
            .Include(cita => cita.Paciente)
            .Include(cita => cita.Odontologo);

        if (desde.HasValue)
        {
            query = query.Where(cita => cita.Fecha >= desde.Value);
        }

        if (hasta.HasValue)
        {
            query = query.Where(cita => cita.Fecha <= hasta.Value);
        }

        if (odontologoId.HasValue)
        {
            query = query.Where(cita => cita.OdontologoId == odontologoId.Value);
        }

        if (estado.HasValue)
        {
            query = query.Where(cita => cita.Estado == estado.Value);
        }

        return await query
            .OrderBy(cita => cita.Fecha)
            .ThenBy(cita => cita.HoraInicio)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Paciente>> GetPacientesAsync(
        DateTime? desde,
        DateTime? hasta,
        EstadoRegistro? estado,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Paciente> query = dbContext.Pacientes.AsNoTracking();

        if (desde.HasValue)
        {
            query = query.Where(paciente => paciente.FechaRegistro >= desde.Value);
        }

        if (hasta.HasValue)
        {
            // El "hasta" llega como fecha sin hora: se incluye todo ese día.
            var limiteExclusivo = hasta.Value.Date.AddDays(1);
            query = query.Where(paciente => paciente.FechaRegistro < limiteExclusivo);
        }

        if (estado.HasValue)
        {
            query = query.Where(paciente => paciente.Estado == estado.Value);
        }

        return await query
            .OrderBy(paciente => paciente.Apellidos)
            .ThenBy(paciente => paciente.Nombres)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TratamientoRealizado>> GetTratamientosAsync(
        DateTime? desde,
        DateTime? hasta,
        int? odontologoId,
        EstadoTratamiento? estado,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TratamientoRealizado> query = dbContext.TratamientosRealizados
            .AsNoTracking()
            .Include(tratamiento => tratamiento.Paciente)
            .Include(tratamiento => tratamiento.Tratamiento)
            .Include(tratamiento => tratamiento.Odontologo)
            .Include(tratamiento => tratamiento.PiezaDental);

        if (desde.HasValue)
        {
            query = query.Where(tratamiento => tratamiento.Fecha >= desde.Value);
        }

        if (hasta.HasValue)
        {
            var limiteExclusivo = hasta.Value.Date.AddDays(1);
            query = query.Where(tratamiento => tratamiento.Fecha < limiteExclusivo);
        }

        if (odontologoId.HasValue)
        {
            query = query.Where(tratamiento => tratamiento.OdontologoId == odontologoId.Value);
        }

        if (estado.HasValue)
        {
            query = query.Where(tratamiento => tratamiento.Estado == estado.Value);
        }

        return await query
            .OrderByDescending(tratamiento => tratamiento.Fecha)
            .ThenByDescending(tratamiento => tratamiento.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Pago>> GetPagosAsync(
        DateTime? desde,
        DateTime? hasta,
        MetodoPago? metodo,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Pago> query = dbContext.Pagos
            .AsNoTracking()
            .Include(pago => pago.TratamientoRealizado)
                .ThenInclude(tratamiento => tratamiento.Paciente)
            .Include(pago => pago.TratamientoRealizado)
                .ThenInclude(tratamiento => tratamiento.Tratamiento)
            .Include(pago => pago.Usuario);

        if (desde.HasValue)
        {
            query = query.Where(pago => pago.Fecha >= desde.Value);
        }

        if (hasta.HasValue)
        {
            var limiteExclusivo = hasta.Value.Date.AddDays(1);
            query = query.Where(pago => pago.Fecha < limiteExclusivo);
        }

        if (metodo.HasValue)
        {
            query = query.Where(pago => pago.MetodoPago == metodo.Value);
        }

        return await query
            .OrderByDescending(pago => pago.Fecha)
            .ThenByDescending(pago => pago.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Odontologo>> GetOdontologosAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Odontologos
            .AsNoTracking()
            .OrderBy(odontologo => odontologo.Apellidos)
            .ThenBy(odontologo => odontologo.Nombres)
            .ToListAsync(cancellationToken);
}
