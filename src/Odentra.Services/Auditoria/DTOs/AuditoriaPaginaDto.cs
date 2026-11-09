namespace Odentra.Services.Auditoria.DTOs;

/// <summary>Resultado paginado de la búsqueda de auditoría.</summary>
public sealed record AuditoriaPaginaDto(
    IReadOnlyList<AuditoriaRegistroDto> Items,
    int Total,
    int Pagina,
    int TamanoPagina)
{
    public int TotalPaginas => TamanoPagina <= 0
        ? 0
        : (int)Math.Ceiling(Total / (double)TamanoPagina);

    public int DesdeRegistro => Total == 0
        ? 0
        : ((Pagina - 1) * TamanoPagina) + 1;

    public int HastaRegistro => Total == 0
        ? 0
        : Math.Min(Total, Pagina * TamanoPagina);
}
