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
