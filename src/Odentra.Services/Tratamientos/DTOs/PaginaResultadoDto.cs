namespace Odentra.Services.Tratamientos.DTOs;

/// <summary>
/// Resultado genérico de una consulta paginada.
/// La UI usa Items para pintar filas y TotalRegistros/TotalPaginas para la paginación.
/// </summary>
/// <typeparam name="T">Tipo de cada elemento de la página (normalmente un DTO de fila).</typeparam>
/// <param name="Items">Elementos de la página actual.</param>
/// <param name="TotalRegistros">Total de coincidencias en la base de datos.</param>
/// <param name="Pagina">Número de página actual (1-based).</param>
/// <param name="TamanoPagina">Cantidad máxima de elementos por página.</param>
public sealed record PaginaResultadoDto<T>(
    IReadOnlyList<T> Items,
    int TotalRegistros,
    int Pagina,
    int TamanoPagina)
{
    /// <summary>
    /// Cantidad total de páginas. Si no hay registros devuelve 1
    /// para que la UI siempre pueda mostrar una página válida.
    /// </summary>
    public int TotalPaginas => TotalRegistros == 0
        ? 1
        : (int)Math.Ceiling(TotalRegistros / (double)TamanoPagina);

    /// <summary>Hay una página anterior disponible.</summary>
    public bool HayPaginaAnterior => Pagina > 1;

    /// <summary>Hay una página siguiente disponible.</summary>
    public bool HayPaginaSiguiente => Pagina < TotalPaginas;
}
