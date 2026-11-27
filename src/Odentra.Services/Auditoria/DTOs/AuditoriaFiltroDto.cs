namespace Odentra.Services.Auditoria.DTOs;

/// <summary>
/// Filtros de la consulta de auditoría. Las fechas son fechas de calendario
/// locales; el servicio las convierte a UTC antes de consultar la base.
/// </summary>
public sealed class AuditoriaFiltroDto
{
    public string? Texto { get; set; }
    public string? UsuarioId { get; set; }
    public string? Accion { get; set; }
    public string? Modulo { get; set; }
    public string? Entidad { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    public bool TieneFiltros =>
        !string.IsNullOrWhiteSpace(Texto) ||
        !string.IsNullOrWhiteSpace(UsuarioId) ||
        !string.IsNullOrWhiteSpace(Accion) ||
        !string.IsNullOrWhiteSpace(Modulo) ||
        !string.IsNullOrWhiteSpace(Entidad) ||
        Desde.HasValue ||
        Hasta.HasValue;

    public AuditoriaFiltroDto Clonar() => new()
    {
        Texto = Texto,
        UsuarioId = UsuarioId,
        Accion = Accion,
        Modulo = Modulo,
        Entidad = Entidad,
        Desde = Desde,
        Hasta = Hasta
    };
}
