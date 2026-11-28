using Odentra.Services.Reportes.DTOs;

namespace Odentra.Services.Reportes;

/// <summary>
/// Genera el documento PDF de un reporte. Es de solo lectura: consulta el mismo
/// resultado que usa la pantalla y lo dibuja en un documento listo para imprimir.
/// </summary>
public interface IReportePdfService
{
    Task<ReportePdfDto> GenerarAsync(
        ReporteTipo tipo,
        ReporteFiltroDto filtro,
        string? usuario,
        CancellationToken cancellationToken = default);
}

/// <summary>Contenido binario del PDF y el nombre sugerido del archivo.</summary>
public sealed record ReportePdfDto(byte[] Contenido, string NombreArchivo);
