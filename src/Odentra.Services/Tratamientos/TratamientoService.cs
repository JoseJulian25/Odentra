using Microsoft.EntityFrameworkCore;
using Odentra.Data;
using Odentra.Data.Entities;
using Odentra.Data.Repositories;
using Odentra.Services.Auditoria;

namespace Odentra.Services.Tratamientos;

public sealed class TratamientoService(
    ApplicationDbContext dbContext,
    ITratamientoRepository repository,
    IAuditoriaService auditoriaService) : ITratamientoService
{
    public Task<IReadOnlyList<Tratamiento>> SearchCatalogAsync(
        string? searchTerm,
        EstadoRegistro? estado,
        CancellationToken cancellationToken = default) =>
        repository.SearchAsync(searchTerm, estado, cancellationToken);

    public async Task<IReadOnlyList<TratamientoRealizado>> SearchRealizadosAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TratamientosRealizados
            .Include(item => item.Paciente)
            .Include(item => item.Tratamiento)
            .Include(item => item.Odontologo)
            .Include(item => item.PiezaDental)
            .AsNoTracking()
            .OrderByDescending(item => item.Fecha)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Paciente>> GetPacientesActivosAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Pacientes
            .AsNoTracking()
            .Where(item => item.Estado == EstadoRegistro.Activo)
            .OrderBy(item => item.Apellidos)
            .ThenBy(item => item.Nombres)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Odontologo>> GetOdontologosActivosAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Odontologos
            .AsNoTracking()
            .Where(item => item.Estado == EstadoRegistro.Activo)
            .OrderBy(item => item.Apellidos)
            .ThenBy(item => item.Nombres)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PiezaDental>> GetPiezasDentalesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.PiezasDentales
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);

    public async Task SaveCatalogAsync(
        Tratamiento model,
        EstadoRegistro? estadoOriginal = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(model.Nombre))
            throw new InvalidOperationException("El nombre del tratamiento es obligatorio.");

        if (model.PrecioBase <= 0)
            throw new InvalidOperationException("El precio base debe ser mayor a 0.");

        var nombre = model.Nombre.Trim();
        var duplicate = await dbContext.Tratamientos
            .AsNoTracking()
            .AnyAsync(item =>
                item.Id != model.Id &&
                item.Nombre == nombre,
                cancellationToken);

        if (duplicate)
            throw new InvalidOperationException("Ya existe un tratamiento con ese nombre.");

        model.Nombre = nombre;
        var isNew = model.Id == 0;
        if (isNew)
        {
            await repository.AddAsync(model, cancellationToken);
        }
        else
        {
            var existing = await repository.GetByIdAsync(model.Id, cancellationToken)
                ?? throw new InvalidOperationException("El tratamiento no existe.");

            existing.Nombre = model.Nombre;
            existing.Descripcion = model.Descripcion;
            existing.PrecioBase = model.PrecioBase;
            existing.Estado = model.Estado;
            model = existing;
            await repository.UpdateAsync(existing, cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);

        var action = isNew ? "Agregó" : "Modificó";
        if (!isNew && estadoOriginal.HasValue && estadoOriginal != model.Estado)
            action = model.Estado == EstadoRegistro.Activo ? "Activó" : "Desactivó";

        await auditoriaService.RegistrarAsync(
            action,
            "Tratamientos",
            nameof(Tratamiento),
            model.Id,
            $"{action} el tratamiento: {model.Nombre} — RD${model.PrecioBase:N2}",
            cancellationToken);
    }

    public async Task CreateRealizadoAsync(
        TratamientoRealizado model,
        CancellationToken cancellationToken = default)
    {
        if (model.PacienteId <= 0)
            throw new InvalidOperationException("Debe seleccionar un paciente.");
        if (model.TratamientoId <= 0)
            throw new InvalidOperationException("Debe seleccionar un tratamiento.");
        if (model.OdontologoId <= 0)
            throw new InvalidOperationException("Debe seleccionar un odontólogo.");
        if (model.Fecha == default)
            throw new InvalidOperationException("La fecha del tratamiento no es válida.");
        if (model.Precio <= 0)
            throw new InvalidOperationException("El precio debe ser mayor a 0.");

        var paciente = await dbContext.Pacientes
            .FirstOrDefaultAsync(item => item.Id == model.PacienteId, cancellationToken);
        if (paciente is null || paciente.Estado != EstadoRegistro.Activo)
            throw new InvalidOperationException("El paciente debe existir y estar activo.");

        var tratamiento = await dbContext.Tratamientos
            .FirstOrDefaultAsync(item => item.Id == model.TratamientoId, cancellationToken);
        if (tratamiento is null || tratamiento.Estado != EstadoRegistro.Activo)
            throw new InvalidOperationException("El tratamiento debe existir y estar activo.");

        var odontologo = await dbContext.Odontologos
            .FirstOrDefaultAsync(item => item.Id == model.OdontologoId, cancellationToken);
        if (odontologo is null || odontologo.Estado != EstadoRegistro.Activo)
            throw new InvalidOperationException("El odontólogo debe existir y estar activo.");

        if (model.PiezaDentalId.HasValue &&
            !await dbContext.PiezasDentales.AnyAsync(
                item => item.Id == model.PiezaDentalId.Value,
                cancellationToken))
        {
            throw new InvalidOperationException("La pieza dental seleccionada no existe.");
        }

        dbContext.TratamientosRealizados.Add(model);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditoriaService.RegistrarAsync(
            "Registró",
            "Tratamientos",
            nameof(TratamientoRealizado),
            model.Id,
            $"Registró el tratamiento {tratamiento.Nombre} para {paciente.Nombres} {paciente.Apellidos} — RD${model.Precio:N2} ({model.Estado})",
            cancellationToken);
    }
}
