using System.Globalization;
using Odentra.Data.Entities;

namespace Odentra.UI.Formatting;

/// <summary>
/// Helpers de presentación del módulo de Tratamientos.
/// Centralizar el formato evita que cada pantalla muestre precios o fechas distinto.
/// </summary>
public static class Formato
{
    // Cultura es-DO: separador de miles con coma y decimales con punto, igual que en las pantallas.
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-DO");

    /// <summary>
    /// Formatea un monto como moneda dominicana.
    /// "#,##0.##" muestra "RD$2,000" cuando no hay centavos y "RD$2,000.50" cuando sí los hay.
    /// </summary>
    public static string Moneda(decimal valor) =>
        $"RD${valor.ToString("#,##0.##", Cultura)}";

    /// <summary>
    /// Convierte una fecha almacenada en UTC a la hora local del servidor
    /// y la muestra como dd/MM/yyyy (formato usado en las tablas y el detalle).
    /// </summary>
    public static string Fecha(DateTime fecha) =>
        fecha.ToLocalTime().ToString("dd/MM/yyyy", Cultura);

    /// <summary>
    /// Igual que <see cref="Fecha"/> pero incluye la hora (útil para pagos u otros registros).
    /// </summary>
    public static string FechaHora(DateTime fecha) =>
        fecha.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Cultura);

    /// <summary>
    /// Traduce el enum <see cref="EstadoTratamiento"/> al texto que se muestra al usuario.
    /// "EnProceso" se muestra como "En proceso" (con espacio y minúscula).
    /// </summary>
    public static string Estado(EstadoTratamiento estado) => estado switch
    {
        EstadoTratamiento.Pendiente => "Pendiente",
        EstadoTratamiento.EnProceso => "En proceso",
        EstadoTratamiento.Completado => "Completado",
        EstadoTratamiento.Cancelado => "Cancelado",
        _ => estado.ToString()
    };
}
