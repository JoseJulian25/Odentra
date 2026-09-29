using Odentra.Data.Entities;
using Odentra.Services.Odontologos.DTOs;

namespace Odentra.Services.Odontologos;

public interface IOdontologoService
{
    Task<IReadOnlyList<OdontologoListItemDto>> SearchAsync(string? searchTerm, EstadoRegistro? estado, CancellationToken cancellationToken = default);
    Task<OdontologoDetalleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OdontologoFormDto?> GetFormAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(OdontologoFormDto model, CancellationToken cancellationToken = default);
    Task UpdateAsync(OdontologoFormDto model, CancellationToken cancellationToken = default);
    Task SetStatusAsync(int id, EstadoRegistro estado, CancellationToken cancellationToken = default);
}
