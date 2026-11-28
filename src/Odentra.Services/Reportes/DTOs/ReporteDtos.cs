namespace Odentra.Services.Reportes.DTOs;

/// <summary>Reportes disponibles en la pantalla /reportes.</summary>
public enum ReporteTipo
{
    Citas,
    Pacientes,
    Tratamientos,
    Pagos
}

/// <summary>
/// Cómo debe presentarse una columna: texto normal, número, dinero, fecha o
/// estado. La pantalla y el PDF usan esta información para alinear y colorear.
/// </summary>
public enum TipoColumna
{
    Texto,
    Numero,
    Dinero,
    Fecha,
    Estado
}

/// <summary>
/// Filtros comunes de los reportes. Cada reporte usa los que le corresponden:
/// las fechas siempre, el odontólogo en citas y tratamientos, y el estado
/// (o método de pago) según el tipo de reporte.
/// </summary>
public sealed record ReporteFiltroDto(
    DateTime? Desde,
    DateTime? Hasta,
    int? OdontologoId,
    string? Estado);

/// <summary>Tarjeta de resumen (KPI) del reporte.</summary>
/// <param name="Icono">Clave del icono que dibuja la interfaz.</param>
public sealed record ReporteTarjetaDto(
    string Etiqueta,
    string Valor,
    string Tono,
    string Icono);

/// <summary>Fila del gráfico de distribución (por estado o método de pago).</summary>
public sealed record ReporteSerieDto(
    string Etiqueta,
    int Cantidad,
    string Tono);

/// <summary>Filtro que realmente se aplicó, para mostrarlo en el documento.</summary>
public sealed record ReporteFiltroAplicadoDto(
    string Etiqueta,
    string Valor);

/// <summary>Definición de una columna de la tabla de resultados.</summary>
public sealed record ReporteColumnaDto(
    string Titulo,
    TipoColumna Tipo = TipoColumna.Texto);

/// <summary>
/// Una celda ya formateada. Si <paramref name="Tono"/> tiene valor, la interfaz
/// la pinta como una etiqueta de estado.
/// </summary>
public sealed record ReporteCeldaDto(
    string Texto,
    string? Tono = null);

/// <summary>Una fila de la tabla de resultados.</summary>
public sealed record ReporteFilaDto(
    IReadOnlyList<ReporteCeldaDto> Celdas);

/// <summary>
/// Resultado listo para pintar: metadatos del documento, tarjetas, serie para
/// el gráfico, columnas y filas. Los valores ya vienen formateados (fechas,
/// dinero y estados) para que la pantalla y el PDF solo se encarguen de dibujar.
/// </summary>
public sealed record ReporteResultadoDto(
    ReporteTipo Tipo,
    string Titulo,
    string Descripcion,
    string Periodo,
    IReadOnlyList<ReporteFiltroAplicadoDto> Filtros,
    IReadOnlyList<ReporteTarjetaDto> Tarjetas,
    string TituloSeries,
    IReadOnlyList<ReporteSerieDto> Series,
    IReadOnlyList<ReporteColumnaDto> Columnas,
    IReadOnlyList<ReporteFilaDto> Filas,
    string? TotalEtiqueta,
    string? TotalValor,
    DateTime GeneradoEn);

/// <summary>Opción para los filtros (por ahora, odontólogos).</summary>
public sealed record ReporteOpcionDto(int Id, string Nombre);
