namespace Odentra.Data.Repositories;

/// <summary>
/// Criterios de búsqueda de la auditoría. Los valores de fecha llegan ya
/// convertidos a UTC para que la comparación con la columna FechaHora sea
/// correcta sin importar la zona horaria del servidor.
/// </summary>
public sealed record AuditoriaQuery(
    string? Texto,
    string? UsuarioId,
    string? Accion,
    string? Modulo,
    string? Entidad,
    DateTime? DesdeUtc,
    DateTime? HastaUtc);

/// <summary>Una página de registros de auditoría junto con el total filtrado.</summary>
public sealed record AuditoriaPaginaData(
    IReadOnlyList<Odentra.Data.Entities.RegistroAuditoria> Items,
    int Total,
    int Pagina,
    int TamanoPagina);

/// <summary>Números globales que resumen la actividad auditada.</summary>
public sealed record AuditoriaResumenData(
    int Total,
    int Hoy,
    int UltimosSieteDias,
    int UsuariosDistintos,
    string? ModuloMasActivo,
    int EventosModuloMasActivo);
