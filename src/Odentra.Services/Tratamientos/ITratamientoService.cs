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
    /// Carga las listas necesarias para el formulario de registro:
    /// pacientes activos, odontólogos activos, piezas dentales y tratamientos activos.
    /// </summary>
    Task<CatalogosRealizadoDto> GetCatalogosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra un tratamiento aplicado a un paciente y devuelve el Id generado.
    /// Valida fecha, paciente, odontólogo, pieza y que el tratamiento esté activo.
    /// </summary>
    Task<int> CreateRealizadoAsync(TratamientoRealizadoFormDto model, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene el detalle de un tratamiento realizado, incluidos sus pagos.
    /// Devuelve null si no existe.
    /// </summary>
    Task<TratamientoDetalleDto?> GetDetalleAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cambia el estado clínico de un tratamiento realizado.
    /// No permite cancelar si ya tiene pagos registrados.
    /// </summary>
    Task SetEstadoRealizadoAsync(int id, EstadoTratamiento estado, CancellationToken cancellationToken = default);
}

using Odentra.Data.Entities;

namespace Odentra.Services.Tratamientos;

public interface ITratamientoService
{
    Task<IReadOnlyList<Tratamiento>> SearchCatalogAsync(
        string? searchTerm,
        EstadoRegistro? estado,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TratamientoRealizado>> SearchRealizadosAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Paciente>> GetPacientesActivosAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Odontologo>> GetOdontologosActivosAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PiezaDental>> GetPiezasDentalesAsync(
        CancellationToken cancellationToken = default);

    Task SaveCatalogAsync(
        Tratamiento model,
        EstadoRegistro? estadoOriginal = null,
        CancellationToken cancellationToken = default);

    Task CreateRealizadoAsync(
        TratamientoRealizado model,
        CancellationToken cancellationToken = default);
}
