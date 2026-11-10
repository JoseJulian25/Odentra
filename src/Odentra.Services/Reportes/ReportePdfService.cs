using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Odentra.Services.Reportes.DTOs;

namespace Odentra.Services.Reportes;

/// <summary>
/// Dibuja el PDF de un reporte con QuestPDF.
///
/// El documento tiene la estructura de un reporte formal:
///   - Encabezado con la marca, el título y el período.
///   - Metadatos: fecha de generación, usuario y cantidad de registros.
///   - Tarjetas de resumen (KPI).
///   - Distribución por estado o método de pago.
///   - Tabla de detalle con totales y numeración de páginas.
///
/// La pantalla y el PDF comparten el mismo <see cref="ReporteResultadoDto"/>, así
/// que ambos muestran exactamente la misma información.
/// </summary>
public sealed class ReportePdfService(IReporteService reporteService) : IReportePdfService
{
    private static readonly CultureInfo Cultura = new("es-DO");

    static ReportePdfService()
    {
        // QuestPDF exige declarar la licencia antes de generar documentos.
        // Community cubre el uso de este proyecto.
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.EnableDetailedLayoutErrors = false;
    }

    public async Task<ReportePdfDto> GenerarAsync(
        ReporteTipo tipo,
        ReporteFiltroDto filtro,
        string? usuario,
        CancellationToken cancellationToken = default)
    {
        var resultado = await reporteService.GenerarAsync(tipo, filtro, cancellationToken);
        var contenido = new DocumentoReporte(resultado, usuario).GeneratePdf();

        var nombre = $"Reporte-{resultado.Tipo}-{DateTime.Now:yyyyMMdd-HHmm}.pdf";

        return new ReportePdfDto(contenido, nombre);
    }

    // =========================================================================================
    // DOCUMENTO
    // =========================================================================================

    private sealed class DocumentoReporte(
        ReporteResultadoDto resultado,
        string? usuario) : IDocument
    {
        private static readonly Color Navy = Color.FromHex("#0F172A");
        private static readonly Color Blue = Color.FromHex("#2563EB");
        private static readonly Color Slate = Color.FromHex("#64748B");
        private static readonly Color SoftSlate = Color.FromHex("#94A3B8");
        private static readonly Color Border = Color.FromHex("#E2E8F0");
        private static readonly Color Soft = Color.FromHex("#F8FAFC");

        public DocumentMetadata GetMetadata() => new()
        {
            Title = resultado.Titulo,
            Author = "Odentra",
            Subject = resultado.Descripcion,
            Creator = "Odentra · Sistema de gestión odontológica"
        };

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(texto => texto
                    .FontSize(8)
                    .FontColor(Color.FromHex("#334155")));

                page.Header().Element(Encabezado);
                page.Content().Element(Contenido);
                page.Footer().Element(PieDePagina);
            });
        }

        // -----------------------------------------------------------------------------------
        // Encabezado
        // -----------------------------------------------------------------------------------

        private void Encabezado(IContainer container)
        {
            container.Column(columna =>
            {
                columna.Item().Row(fila =>
                {
                    fila.RelativeItem().Row(marca =>
                    {
                        marca.ConstantItem(32)
                            .Height(32)
                            .Background(Blue)
                            .AlignMiddle()
                            .AlignCenter()
                            .Text("O")
                            .FontSize(16)
                            .Bold()
                            .FontColor(Colors.White);

                        marca.ConstantItem(9);

                        marca.RelativeItem().Column(texto =>
                        {
                            texto.Item().Text("Odentra").FontSize(15).Bold().FontColor(Navy);
                            texto.Item().Text("Sistema de gestión odontológica")
                                .FontSize(7)
                                .FontColor(Slate);
                        });
                    });

                    fila.ConstantItem(330).AlignRight().Column(texto =>
                    {
                        texto.Item().AlignRight()
                            .Text("REPORTE")
                            .FontSize(7)
                            .Bold()
                            .FontColor(Blue);

                        texto.Item().AlignRight()
                            .Text(resultado.Titulo)
                            .FontSize(15)
                            .Bold()
                            .FontColor(Navy);

                        texto.Item().AlignRight()
                            .Text(resultado.Periodo)
                            .FontSize(8)
                            .FontColor(Slate);
                    });
                });

                columna.Item().PaddingTop(8).LineHorizontal(2).LineColor(Blue);

                columna.Item().PaddingTop(6).Row(fila =>
                {
                    fila.RelativeItem()
                        .Text($"Generado: {resultado.GeneradoEn.ToString("dd/MM/yyyy HH:mm", Cultura)}")
                        .FontSize(7)
                        .FontColor(Slate);

                    fila.RelativeItem()
                        .Text($"Usuario: {(string.IsNullOrWhiteSpace(usuario) ? "Sistema" : usuario)}")
                        .FontSize(7)
                        .FontColor(Slate);

                    fila.ConstantItem(130).AlignRight()
                        .Text(Registros(resultado.Filas.Count))
                        .FontSize(7)
                        .Bold()
                        .FontColor(Navy);
                });
            });
        }

        // -----------------------------------------------------------------------------------
        // Contenido
        // -----------------------------------------------------------------------------------

        private void Contenido(IContainer container)
        {
            container.Column(columna =>
            {
                columna.Spacing(10);

                columna.Item().PaddingTop(10).Element(Filtros);
                columna.Item().Element(Tarjetas);

                if (resultado.Series.Count > 0)
                {
                    columna.Item().Element(Distribucion);
                }

                columna.Item().Element(Tabla);
            });
        }

        private void Filtros(IContainer container)
        {
            container
                .Background(Soft)
                .Border(1)
                .BorderColor(Border)
                .PaddingVertical(6)
                .PaddingHorizontal(8)
                .Text(string.Join("      ", resultado.Filtros.Select(filtro => $"{filtro.Etiqueta}: {filtro.Valor}")))
                .FontSize(7)
                .FontColor(Slate);
        }

        private void Tarjetas(IContainer container)
        {
            container.Row(fila =>
            {
                foreach (var tarjeta in resultado.Tarjetas)
                {
                    fila.RelativeItem()
                        .PaddingRight(6)
                        .Background(Soft)
                        .Border(1)
                        .BorderColor(Border)
                        .Padding(8)
                        .Column(columna =>
                        {
                            columna.Item()
                                .Text(tarjeta.Etiqueta.ToUpperInvariant())
                                .FontSize(6.5f)
                                .FontColor(Slate);

                            columna.Item().PaddingTop(2)
                                .Text(tarjeta.Valor)
                                .FontSize(13)
                                .Bold()
                                .FontColor(Tono(tarjeta.Tono));
                        });
                }
            });
        }

        private void Distribucion(IContainer container)
        {
            var total = resultado.Series.Sum(serie => serie.Cantidad);

            container
                .Border(1)
                .BorderColor(Border)
                .Padding(9)
                .Column(columna =>
                {
                    columna.Item()
                        .Text(resultado.TituloSeries)
                        .FontSize(8)
                        .Bold()
                        .FontColor(Navy);

                    columna.Item().PaddingTop(6).Column(barras =>
                    {
                        barras.Spacing(4);

                        foreach (var serie in resultado.Series)
                        {
                            var porcentaje = total > 0
                                ? (float)serie.Cantidad * 100f / total
                                : 0f;

                            barras.Item().Row(fila =>
                            {
                                fila.ConstantItem(120)
                                    .Text(serie.Etiqueta)
                                    .FontSize(7)
                                    .FontColor(Color.FromHex("#334155"));

                                fila.RelativeItem().AlignMiddle().Height(7).Element(barra =>
                                {
                                    barra.Background(Color.FromHex("#EEF2F7")).Row(relleno =>
                                    {
                                        if (porcentaje >= 100f)
                                        {
                                            relleno.RelativeItem().Background(Tono(serie.Tono));
                                        }
                                        else
                                        {
                                            relleno.RelativeItem(Math.Max(porcentaje, 1f)).Background(Tono(serie.Tono));
                                            relleno.RelativeItem(Math.Max(100f - porcentaje, 0f));
                                        }
                                    });
                                });

                                fila.ConstantItem(70).AlignRight()
                                    .Text($"{serie.Cantidad} ({porcentaje.ToString("0", Cultura)}%)")
                                    .FontSize(7)
                                    .FontColor(Slate);
                            });
                        }
                    });
                });
        }

        private void Tabla(IContainer container)
        {
            container
                .Border(1)
                .BorderColor(Border)
                .Column(columna =>
                {
                    columna.Item()
                        .Background(Soft)
                        .PaddingVertical(6)
                        .PaddingHorizontal(8)
                        .Row(encabezado =>
                        {
                            encabezado.RelativeItem()
                                .Text("Detalle de resultados")
                                .FontSize(8)
                                .Bold()
                                .FontColor(Navy);

                            encabezado.ConstantItem(100).AlignRight()
                                .Text(Registros(resultado.Filas.Count))
                                .FontSize(7)
                                .FontColor(Slate);
                        });

                    if (resultado.Filas.Count == 0)
                    {
                        columna.Item()
                            .Padding(24)
                            .AlignCenter()
                            .Text("No hay datos para los filtros seleccionados.")
                            .FontSize(8)
                            .FontColor(Slate);

                        return;
                    }

                    columna.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            foreach (var columnaTabla in resultado.Columnas)
                            {
                                columnas.RelativeColumn(PesoColumna(columnaTabla.Tipo));
                            }
                        });

                        tabla.Header(encabezado =>
                        {
                            foreach (var columnaTabla in resultado.Columnas)
                            {
                                encabezado.Cell()
                                    .Background(Navy)
                                    .PaddingVertical(5)
                                    .PaddingHorizontal(4)
                                    .Text(columnaTabla.Titulo.ToUpperInvariant())
                                    .FontSize(6.5f)
                                    .Bold()
                                    .FontColor(Colors.White);
                            }
                        });

                        var indice = 0;

                        foreach (var fila in resultado.Filas)
                        {
                            var fondo = indice % 2 == 0 ? Colors.White : Soft;

                            for (var posicion = 0; posicion < fila.Celdas.Count; posicion++)
                            {
                                var celda = fila.Celdas[posicion];
                                var tipo = resultado.Columnas[posicion].Tipo;

                                var contenedor = tabla.Cell()
                                    .Background(fondo)
                                    .BorderBottom(1)
                                    .BorderColor(Color.FromHex("#F1F5F9"))
                                    .PaddingVertical(4)
                                    .PaddingHorizontal(4);

                                contenedor = tipo is TipoColumna.Dinero or TipoColumna.Numero
                                    ? contenedor.AlignRight()
                                    : contenedor.AlignLeft();

                                var texto = contenedor.Text(celda.Texto).FontSize(7);

                                if (!string.IsNullOrWhiteSpace(celda.Tono))
                                {
                                    texto.Bold().FontColor(Tono(celda.Tono));
                                }
                                else
                                {
                                    texto.FontColor(Color.FromHex("#475569"));
                                }
                            }

                            indice++;
                        }

                        if (!string.IsNullOrWhiteSpace(resultado.TotalEtiqueta))
                        {
                            tabla.Cell()
                                .ColumnSpan((uint)resultado.Columnas.Count)
                                .Background(Color.FromHex("#F1F5F9"))
                                .PaddingVertical(6)
                                .PaddingHorizontal(6)
                                .Row(total =>
                                {
                                    total.RelativeItem()
                                        .Text(resultado.TotalEtiqueta)
                                        .FontSize(7)
                                        .Bold()
                                        .FontColor(Slate);

                                    total.ConstantItem(160).AlignRight()
                                        .Text(resultado.TotalValor ?? string.Empty)
                                        .FontSize(8)
                                        .Bold()
                                        .FontColor(Navy);
                                });
                        }
                    });
                });
        }

        // -----------------------------------------------------------------------------------
        // Pie de página
        // -----------------------------------------------------------------------------------

        private void PieDePagina(IContainer container)
        {
            container.Column(columna =>
            {
                columna.Item().LineHorizontal(1).LineColor(Border);

                columna.Item().PaddingTop(5).Row(fila =>
                {
                    fila.RelativeItem()
                        .Text("Odentra · Documento generado automáticamente. Información confidencial.")
                        .FontSize(6.5f)
                        .FontColor(SoftSlate);

                    fila.ConstantItem(90).AlignRight()
                        .Text(texto =>
                        {
                            texto.DefaultTextStyle(estilo => estilo.FontSize(6.5f).FontColor(SoftSlate));
                            texto.Span("Página ");
                            texto.CurrentPageNumber();
                            texto.Span(" de ");
                            texto.TotalPages();
                        });
                });
            });
        }

        // -----------------------------------------------------------------------------------
        // Ayudantes visuales
        // -----------------------------------------------------------------------------------

        private static float PesoColumna(TipoColumna tipo) => tipo switch
        {
            TipoColumna.Fecha => 1.2f,
            TipoColumna.Numero => 1f,
            TipoColumna.Dinero => 1.5f,
            TipoColumna.Estado => 1.3f,
            _ => 2.7f
        };

        private static Color Tono(string tono) => tono switch
        {
            "green" => Color.FromHex("#059669"),
            "red" => Color.FromHex("#DC2626"),
            "amber" => Color.FromHex("#D97706"),
            "blue" => Color.FromHex("#2563EB"),
            "violet" => Color.FromHex("#7C3AED"),
            _ => Slate
        };

        private static string Registros(int cantidad) =>
            cantidad == 1 ? "1 registro" : $"{cantidad.ToString("N0", Cultura)} registros";
    }
}
