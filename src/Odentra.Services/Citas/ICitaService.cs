using Odentra.Data.Entities;
using Odentra.Services.Citas.DTOs;

namespace Odentra.Services.Citas;

public interface ICitaService
{
    Task<IReadOnlyList<CitaListItemDto>> SearchAsync(
        DateOnly? fecha = null,
        int? pacienteId = null,
        int? odontologoId = null,
        EstadoCita? estado = null,
        CancellationToken cancellationToken = default);

    Task<CitaDetalleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CitaFormDto?> GetFormAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CitaFormDto model, CancellationToken cancellationToken = default);
    Task UpdateAsync(CitaFormDto model, CancellationToken cancellationToken = default);
    Task SetEstadoAsync(int id, EstadoCita estado, CancellationToken cancellationToken = default);
}
