using Microsoft.EntityFrameworkCore;
using Odentra.Data.Entities;

namespace Odentra.Data.Repositories;

/// <summary>
/// Implementación del repositorio de Tratamientos usando Entity Framework Core.
/// Recibe el <see cref="ApplicationDbContext"/> por inyección de dependencias
/// (constructor primario) y trabaja contra las tablas del módulo.
/// </summary>
public sealed class TratamientoRepository(ApplicationDbContext dbContext) : ITratamientoRepository
{
    /// <summary>
    /// Consulta paginada del catálogo. Construye la consulta por partes:
    /// primero filtra, luego cuenta el total y finalmente trae solo la página pedida.
    /// </summary>
    public async Task<(IReadOnlyList<Tratamiento> Items, int Total)> SearchAsync(
        string? searchTerm,
        EstadoRegistro? estado,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        // AsNoTracking: son datos solo para mostrar, no se van a modificar.
        IQueryable<Tratamiento> query = dbContext.Tratamientos.AsNoTracking();

        // Filtro opcional por estado (Activo / Inactivo).
        if (estado.HasValue)
        {
            query = query.Where(treatment => treatment.Estado == estado.Value);
        }

        // Filtro opcional por texto en nombre o descripción.
        // ToLower hace la búsqueda sin distinguir mayúsculas/minúsculas.
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedTerm = searchTerm.Trim().ToLower();
            query = query.Where(treatment =>
                treatment.Nombre.ToLower().Contains(normalizedTerm) ||
                (treatment.Descripcion != null && treatment.Descripcion.ToLower().Contains(normalizedTerm)));
        }

        // El total se calcula antes de paginar: es lo que necesita la UI
        // para saber cuántas páginas existen.
        var total = await query.CountAsync(cancellationToken);

        // Skip/Take se traducen a OFFSET/FETCH en SQL Server.
        var items = await query
            .OrderBy(treatment => treatment.Nombre)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <summary>
    /// Comprueba si el nombre ya está registrado. Se compara en minúsculas para
    /// que "Limpieza" y "limpieza" cuenten como duplicado.
    /// </summary>
    public Task<bool> ExistsNombreAsync(
        string nombre,
        int? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = nombre.Trim().ToLower();

        var query = dbContext.Tratamientos
            .AsNoTracking()
            .Where(treatment => treatment.Nombre.ToLower() == normalizedName);

        // Al editar, el propio registro no debe contar como duplicado.
        if (excludedId.HasValue)
        {
            query = query.Where(treatment => treatment.Id != excludedId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    /// <summary>Busca un tratamiento por Id sin dejarlo rastreado por EF (solo lectura).</summary>
    public Task<Tratamiento?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Tratamientos
            .AsNoTracking()
            .FirstOrDefaultAsync(treatment => treatment.Id == id, cancellationToken);

    /// <summary>Agrega el tratamiento al contexto; se inserta al llamar SaveChangesAsync.</summary>
    public async Task AddAsync(Tratamiento tratamiento, CancellationToken cancellationToken = default)
    {
        await dbContext.Tratamientos.AddAsync(tratamiento, cancellationToken);
    }

    /// <summary>
    /// Actualiza un tratamiento existente.
    /// Importante: primero busca la instancia que EF ya está rastreando (o la carga)
    /// y modifica sus propiedades. Así se evita adjuntar una segunda instancia con
    /// el mismo Id, error que ocurre si se crea y edita en el mismo circuito.
    /// </summary>
    public async Task<bool> UpdateAsync(Tratamiento tratamiento, CancellationToken cancellationToken = default)
    {
        var actual = await dbContext.Tratamientos
            .FirstOrDefaultAsync(item => item.Id == tratamiento.Id, cancellationToken);

        if (actual is null)
        {
            return false;
        }

        // Solo se copian los campos editables del catálogo.
        actual.Nombre = tratamiento.Nombre;
        actual.Descripcion = tratamiento.Descripcion;
        actual.PrecioBase = tratamiento.PrecioBase;
        actual.Estado = tratamiento.Estado;
        return true;
    }

    /// <summary>
    /// Consulta paginada de tratamientos realizados.
    /// Incluye las relaciones necesarias para mostrar nombre de paciente,
    /// tratamiento, odontólogo y pieza sin consultas adicionales por fila.
    /// </summary>
    public async Task<(IReadOnlyList<TratamientoRealizado> Items, int Total)> SearchRealizadosAsync(
        string? searchTerm,
        EstadoTratamiento? estado,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TratamientoRealizado> query = dbContext.TratamientosRealizados
            .AsNoTracking()
            .Include(realizado => realizado.Paciente)
            .Include(realizado => realizado.Tratamiento)
            .Include(realizado => realizado.Odontologo)
            .Include(realizado => realizado.PiezaDental);

        // Filtro por estado clínico (Pendiente, En proceso, Completado, Cancelado).
        if (estado.HasValue)
        {
            query = query.Where(realizado => realizado.Estado == estado.Value);
        }

        // Búsqueda por paciente (en ambos órdenes), tratamiento u odontólogo.
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedTerm = searchTerm.Trim().ToLower();
            query = query.Where(realizado =>
                (realizado.Paciente.Nombres + " " + realizado.Paciente.Apellidos).ToLower().Contains(normalizedTerm) ||
                (realizado.Paciente.Apellidos + " " + realizado.Paciente.Nombres).ToLower().Contains(normalizedTerm) ||
                realizado.Tratamiento.Nombre.ToLower().Contains(normalizedTerm) ||
                (realizado.Odontologo.Nombres + " " + realizado.Odontologo.Apellidos).ToLower().Contains(normalizedTerm));
        }

        var total = await query.CountAsync(cancellationToken);

        // Los más recientes primero; el Id desempata registros con la misma fecha.
        var items = await query
            .OrderByDescending(realizado => realizado.Fecha)
            .ThenByDescending(realizado => realizado.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    /// <summary>
    /// Trae un tratamiento realizado con todas sus relaciones, incluidos los pagos.
    /// Se usa para el detalle y el estado financiero.
    /// </summary>
    public Task<TratamientoRealizado?> GetRealizadoAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.TratamientosRealizados
            .AsNoTracking()
            .Include(realizado => realizado.Paciente)
            .Include(realizado => realizado.Tratamiento)
            .Include(realizado => realizado.Odontologo)
            .Include(realizado => realizado.PiezaDental)
            .Include(realizado => realizado.Pagos)
            .FirstOrDefaultAsync(realizado => realizado.Id == id, cancellationToken);

    /// <summary>Agrega el tratamiento realizado al contexto para insertarlo al guardar.</summary>
    public async Task AddRealizadoAsync(TratamientoRealizado realizado, CancellationToken cancellationToken = default)
    {
        await dbContext.TratamientosRealizados.AddAsync(realizado, cancellationToken);
    }

    /// <summary>
    /// Cambia el estado de un tratamiento realizado.
    /// Igual que en <see cref="UpdateAsync"/>, se modifica la instancia rastreada
    /// para evitar conflictos de seguimiento de EF.
    /// </summary>
    public async Task<bool> SetEstadoRealizadoAsync(
        int id,
        EstadoTratamiento estado,
        CancellationToken cancellationToken = default)
    {
        var realizado = await dbContext.TratamientosRealizados
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (realizado is null)
        {
            return false;
        }

        realizado.Estado = estado;
        return true;
    }

    /// <summary>Pacientes activos ordenados por apellido para el selector del formulario.</summary>
    public async Task<IReadOnlyList<Paciente>> GetPacientesActivosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Pacientes
            .AsNoTracking()
            .Where(paciente => paciente.Estado == EstadoRegistro.Activo)
            .OrderBy(paciente => paciente.Apellidos)
            .ThenBy(paciente => paciente.Nombres)
            .ToListAsync(cancellationToken);

    /// <summary>Odontólogos activos ordenados por apellido para el selector del formulario.</summary>
    public async Task<IReadOnlyList<Odontologo>> GetOdontologosActivosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Odontologos
            .AsNoTracking()
            .Where(odontologo => odontologo.Estado == EstadoRegistro.Activo)
            .OrderBy(odontologo => odontologo.Apellidos)
            .ThenBy(odontologo => odontologo.Nombres)
            .ToListAsync(cancellationToken);

    /// <summary>Catálogo completo de piezas dentales, ordenado por número FDI.</summary>
    public async Task<IReadOnlyList<PiezaDental>> GetPiezasAsync(CancellationToken cancellationToken = default) =>
        await dbContext.PiezasDentales
            .AsNoTracking()
            .OrderBy(pieza => pieza.NumeroFDI)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Solo tratamientos activos: la regla del dominio impide usar uno inactivo
    /// al registrar un tratamiento realizado.
    /// </summary>
    public async Task<IReadOnlyList<Tratamiento>> GetActivosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Tratamientos
            .AsNoTracking()
            .Where(treatment => treatment.Estado == EstadoRegistro.Activo)
            .OrderBy(treatment => treatment.Nombre)
            .ToListAsync(cancellationToken);

    /// <summary>Verifica existencia + estado activo del paciente en una sola consulta.</summary>
    public Task<bool> PacienteActivoExisteAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Pacientes
            .AsNoTracking()
            .AnyAsync(paciente => paciente.Id == id && paciente.Estado == EstadoRegistro.Activo, cancellationToken);

    /// <summary>Verifica existencia + estado activo del odontólogo en una sola consulta.</summary>
    public Task<bool> OdontologoActivoExisteAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Odontologos
            .AsNoTracking()
            .AnyAsync(odontologo => odontologo.Id == id && odontologo.Estado == EstadoRegistro.Activo, cancellationToken);

    /// <summary>Verifica que la pieza dental exista (las piezas no tienen estado activo/inactivo).</summary>
    public Task<bool> PiezaExisteAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.PiezasDentales
            .AsNoTracking()
            .AnyAsync(pieza => pieza.Id == id, cancellationToken);

    /// <summary>Persiste todos los cambios pendientes del contexto.</summary>
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
