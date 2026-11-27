using Odentra.Services.Auditoria.DTOs;

namespace Odentra.Services.Auditoria;

public interface IAuditoriaService
{
    /// <summary>
    /// Registra un evento atribuido al usuario de la sesión actual.
    /// </summary>
    Task RegistrarAsync(
        string accion,
        string modulo,
        string entidad,
        int? entidadId,
        string descripcion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registra un evento de sesión (inicio, cierre o intento fallido) cuando
    /// todavía no existe un usuario interactivo establecido. Se usa desde el
    /// endpoint HTTP de login/logout.
    /// </summary>
    Task RegistrarEventoSesionAsync(
        string usuarioId,
        string accion,
        string descripcion,
        string? direccionIp = null,
        string? agenteUsuario = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ActividadRecienteDto>> GetRecientesAsync(
        int cantidad = 20,
        CancellationToken cancellationToken = default);

    Task<AuditoriaPaginaDto> BuscarAsync(
        AuditoriaFiltroDto filtro,
        int pagina = 1,
        int tamanoPagina = 12,
        CancellationToken cancellationToken = default);

    Task<AuditoriaRegistroDto?> GetDetalleAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<AuditoriaFiltrosDto> GetFiltrosAsync(
        CancellationToken cancellationToken = default);

    Task<AuditoriaResumenDto> GetResumenAsync(
        CancellationToken cancellationToken = default);
}
