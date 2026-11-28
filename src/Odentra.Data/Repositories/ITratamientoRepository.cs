using Odentra.Data.Entities;

namespace Odentra.Data.Repositories;

/// <summary>
/// Contrato de acceso a datos del módulo de Tratamientos.
/// Aquí solo se declaran las operaciones; la implementación con EF Core está en
/// <see cref="TratamientoRepository"/>. Separar la interfaz permite cambiar la
/// implementación (o simularla en pruebas) sin tocar los servicios.
/// </summary>
public interface ITratamientoRepository
{
    /// <summary>
    /// Busca tratamientos del catálogo aplicando texto y estado, con paginación.
    /// Devuelve la página solicitada y el total de coincidencias (para calcular páginas).
    /// </summary>
    Task<(IReadOnlyList<Tratamiento> Items, int Total)> SearchAsync(
        string? searchTerm,
        EstadoRegistro? estado,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Indica si ya existe un tratamiento con ese nombre.
    /// <paramref name="excludedId"/> se usa al editar para no comparar el registro contra sí mismo.
    /// </summary>
    Task<bool> ExistsNombreAsync(
        string nombre,
        int? excludedId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene un tratamiento del catálogo por su identificador (solo lectura).</summary>
    Task<Tratamiento?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Marca un tratamiento nuevo para insertarlo al guardar cambios.</summary>
    Task AddAsync(Tratamiento tratamiento, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copia los datos del tratamiento recibido sobre el registro ya existente.
    /// Devuelve false si el registro no existe. No guarda cambios por sí solo.
    /// </summary>
    Task<bool> UpdateAsync(Tratamiento tratamiento, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca tratamientos realizados (aplicados a pacientes) con texto y estado,
    /// usando paginación. Devuelve la página y el total de coincidencias.
    /// Incluye los pagos para calcular el estado financiero de cada fila.
    /// </summary>
    Task<(IReadOnlyList<TratamientoRealizado> Items, int Total)> SearchRealizadosAsync(
        string? searchTerm,
        EstadoTratamiento? estado,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un tratamiento realizado con sus relaciones (paciente, catálogo,
    /// odontólogo, pieza y pagos) para construir la pantalla de detalle.
    /// </summary>
    Task<TratamientoRealizado?> GetRealizadoAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene un tratamiento realizado rastreado por EF para modificarlo.
    /// Incluye paciente, tratamiento, odontólogo y pagos.
    /// </summary>
    Task<TratamientoRealizado?> GetRealizadoForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Marca un tratamiento realizado nuevo para insertarlo al guardar cambios.</summary>
    Task AddRealizadoAsync(TratamientoRealizado realizado, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cambia únicamente el estado del tratamiento realizado.
    /// Devuelve false si el registro no existe. No guarda cambios por sí solo.
    /// </summary>
    Task<bool> SetEstadoRealizadoAsync(
        int id,
        EstadoTratamiento estado,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene un paciente por Id, sin seguimiento de cambios.</summary>
    Task<Paciente?> GetPacienteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Obtiene un odontólogo por Id, sin seguimiento de cambios.</summary>
    Task<Odontologo?> GetOdontologoAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Lista todos los pacientes (activos e inactivos) para el formulario de edición.</summary>
    Task<IReadOnlyList<Paciente>> GetPacientesAsync(CancellationToken cancellationToken = default);

    /// <summary>Lista todos los odontólogos (activos e inactivos) para el formulario de edición.</summary>
    Task<IReadOnlyList<Odontologo>> GetOdontologosAsync(CancellationToken cancellationToken = default);

    /// <summary>Lista el catálogo de piezas dentales (numeración FDI) para el selector.</summary>
    Task<IReadOnlyList<PiezaDental>> GetPiezasAsync(CancellationToken cancellationToken = default);

    /// <summary>Lista el catálogo completo o solo los activos, según el parámetro.</summary>
    Task<IReadOnlyList<Tratamiento>> GetTratamientosAsync(bool soloActivos, CancellationToken cancellationToken = default);

    /// <summary>Consultas clínicas de un paciente, de la más reciente a la más antigua.</summary>
    Task<IReadOnlyList<ConsultaClinica>> GetConsultasPacienteAsync(int pacienteId, CancellationToken cancellationToken = default);

    /// <summary>Valida que el paciente exista y esté activo antes de registrar un tratamiento.</summary>
    Task<bool> PacienteActivoExisteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Valida que el odontólogo exista y esté activo antes de registrar un tratamiento.</summary>
    Task<bool> OdontologoActivoExisteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Valida que la pieza dental exista cuando el usuario selecciona una.</summary>
    Task<bool> PiezaExisteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Confirma en la base de datos todos los cambios pendientes del DbContext.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
