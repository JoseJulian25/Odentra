using Odentra.Data.Entities;
using Odentra.Services.Tratamientos.DTOs;

namespace Odentra.Services.Tratamientos;

/// <summary>
/// Contrato de la lógica de negocio del módulo de Tratamientos.
/// La UI (componentes Razor) depende de esta interfaz y no del repositorio ni de EF Core.
/// </summary>
public interface ITratamientoService
{
    /// <summary>
    /// Devuelve una página del catálogo de tratamientos, con búsqueda y filtro por estado.
    /// </summary>
    Task<PaginaResultadoDto<TratamientoListItemDto>> SearchCatalogoAsync(
        string? searchTerm,
        EstadoRegistro? estado,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga los datos de un tratamiento en el DTO del formulario de edición.
    /// Devuelve null si el tratamiento no existe.
    /// </summary>
    Task<TratamientoFormDto?> GetFormAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea un tratamiento del catálogo y devuelve el Id generado.
    /// Valida nombre, precio y que el nombre no esté duplicado.
    /// </summary>
    Task<int> CreateAsync(TratamientoFormDto model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un tratamiento existente. Lanza una excepción de negocio
    /// si no existe o si el nombre choca con otro registro.
    /// </summary>
    Task UpdateAsync(TratamientoFormDto model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Devuelve una página de tratamientos realizados, con búsqueda y filtro por estado clínico.
    /// </summary>
    Task<PaginaResultadoDto<TratamientoRealizadoListItemDto>> SearchRealizadosAsync(
        string? searchTerm,
        EstadoTratamiento? estado,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra un tratamiento aplicado a un paciente y devuelve el Id generado.
    /// El estado inicial siempre es En proceso.
    /// </summary>
    Task<int> CreateRealizadoAsync(TratamientoRealizadoFormDto model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el detalle de un tratamiento realizado, con los totales financieros.
    /// Devuelve null si no existe.
    /// </summary>
    Task<TratamientoRealizadoDetalleDto?> GetRealizadoAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Carga los datos de un tratamiento realizado en el DTO del formulario de edición.
    /// Devuelve null si no existe.
    /// </summary>
    Task<TratamientoRealizadoFormDto?> GetRealizadoFormAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Actualiza un tratamiento realizado. No permite editar cancelados ni dejar
    /// el precio por debajo de lo ya pagado.
    /// </summary>
    Task UpdateRealizadoAsync(TratamientoRealizadoFormDto model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancela un tratamiento realizado. No se permite si está completado o si tiene pagos.
    /// </summary>
    Task CancelarRealizadoAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Marca como completado un tratamiento que está en proceso. Los pagos no lo completan.</summary>
    Task MarcarCompletadoAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Vuelve a poner en proceso un tratamiento completado.</summary>
    Task ReabrirRealizadoAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cambia el estado clínico usando la acción correspondiente (completar, reabrir o cancelar).
    /// </summary>
    Task SetEstadoRealizadoAsync(int id, EstadoTratamiento estado, CancellationToken cancellationToken = default);

    /// <summary>Consultas del paciente para asociarlas opcionalmente al tratamiento.</summary>
    Task<IReadOnlyList<ConsultaOptionDto>> GetConsultasPacienteAsync(
        int pacienteId,
        CancellationToken cancellationToken = default);

    /// <summary>Tratamientos del catálogo, activos o todos, para el selector del formulario.</summary>
    Task<IReadOnlyList<TratamientoOptionDto>> GetTratamientosAsync(
        bool soloActivos,
        CancellationToken cancellationToken = default);

    /// <summary>Pacientes para el selector del formulario (incluye inactivos para edición).</summary>
    Task<IReadOnlyList<PacienteOptionDto>> GetPacientesAsync(CancellationToken cancellationToken = default);

    /// <summary>Odontólogos para el selector del formulario (incluye inactivos para edición).</summary>
    Task<IReadOnlyList<OdontologoOptionDto>> GetOdontologosAsync(CancellationToken cancellationToken = default);

    /// <summary>Piezas dentales para el selector del formulario.</summary>
    Task<IReadOnlyList<PiezaDentalOptionDto>> GetPiezasAsync(CancellationToken cancellationToken = default);
}
