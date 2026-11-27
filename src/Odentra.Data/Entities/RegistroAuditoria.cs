namespace Odentra.Data.Entities;

public class RegistroAuditoria
{
    public long Id { get; set; }
    public string UsuarioId { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;
    public string Accion { get; set; } = string.Empty;
    public string Modulo { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public int? EntidadId { get; set; }
    public string? Descripcion { get; set; }

    /// <summary>
    /// Dirección IP desde la que se produjo el evento, cuando se conoce
    /// (por ejemplo, en el inicio y cierre de sesión).
    /// </summary>
    public string? DireccionIp { get; set; }

    /// <summary>
    /// Agente de usuario (navegador) que originó el evento, cuando se conoce.
    /// </summary>
    public string? AgenteUsuario { get; set; }

    public Usuario Usuario { get; set; } = null!;
}