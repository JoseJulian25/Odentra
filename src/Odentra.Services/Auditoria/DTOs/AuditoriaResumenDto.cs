namespace Odentra.Services.Auditoria.DTOs;

/// <summary>Números globales que se muestran en las tarjetas de resumen.</summary>
public sealed record AuditoriaResumenDto(
    int Total,
    int Hoy,
    int UltimosSieteDias,
    int UsuariosDistintos,
    string? ModuloMasActivo,
    int EventosModuloMasActivo);
