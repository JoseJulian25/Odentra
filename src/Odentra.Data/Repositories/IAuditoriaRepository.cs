using Odentra.Data.Entities;

namespace Odentra.Data.Repositories;

public interface IAuditoriaRepository
{
    Task AddAsync(RegistroAuditoria registro, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RegistroAuditoria>> GetRecientesAsync(int cantidad, CancellationToken cancellationToken = default);

    Task<AuditoriaPaginaData> SearchAsync(
        AuditoriaQuery query,
        int pagina,
        int tamanoPagina,
        CancellationToken cancellationToken = default);

    Task<RegistroAuditoria?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Usuario>> GetUsuariosConActividadAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetModulosAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAccionesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetEntidadesAsync(CancellationToken cancellationToken = default);

    Task<AuditoriaResumenData> GetResumenAsync(
        DateTime hoyInicioUtc,
        DateTime mananaInicioUtc,
        DateTime haceSieteDiasUtc,
        CancellationToken cancellationToken = default);
}
