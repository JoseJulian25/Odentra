namespace Odentra.Services.Auditoria.DTOs;

/// <summary>Opción de un filtro (por ejemplo, un usuario o una entidad).</summary>
public sealed record AuditoriaOpcionDto(string Valor, string Texto);

/// <summary>Listas disponibles para llenar los filtros de la pantalla.</summary>
public sealed record AuditoriaFiltrosDto(
    IReadOnlyList<AuditoriaOpcionDto> Usuarios,
    IReadOnlyList<AuditoriaOpcionDto> Acciones,
    IReadOnlyList<AuditoriaOpcionDto> Modulos,
    IReadOnlyList<AuditoriaOpcionDto> Entidades);
