using Odentra.Services.Reportes.DTOs;

namespace Odentra.Services.Reportes;

public interface IReporteService
{
    /// <summary>
    /// Genera el reporte solicitado con los filtros indicados. Solo consulta datos:
    /// no modifica información ni guarda el resultado.
    /// </summary>
    Task<ReporteResultadoDto> GenerarAsync(
        ReporteTipo tipo,
        ReporteFiltroDto filtro,
        CancellationToken cancellationToken = default);

    /// <summary>Odontólogos disponibles para el filtro de citas y tratamientos.</summary>
    Task<IReadOnlyList<ReporteOpcionDto>> GetOdontologosAsync(
        CancellationToken cancellationToken = default);
}
