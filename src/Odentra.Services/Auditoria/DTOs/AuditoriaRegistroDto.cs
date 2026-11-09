namespace Odentra.Services.Auditoria.DTOs;

/// <summary>Una fila del registro de auditoría lista para mostrarse en pantalla.</summary>
public sealed record AuditoriaRegistroDto(
    long Id,
    DateTime FechaHora,
    string Usuario,
    string? UsuarioEmail,
    string Accion,
    string Modulo,
    string Entidad,
    string EntidadLegible,
    int? EntidadId,
    string? Descripcion,
    string? DireccionIp,
    string? AgenteUsuario,
    NivelAuditoria Nivel);
