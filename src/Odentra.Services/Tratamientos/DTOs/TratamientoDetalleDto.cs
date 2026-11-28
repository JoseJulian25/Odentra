using Odentra.Data.Entities;

namespace Odentra.Services.Tratamientos.DTOs;

/// <summary>
/// Pago ya registrado que se muestra en el detalle del tratamiento.
/// Es solo lectura: el alta de pagos pertenece al módulo de Pagos.
/// </summary>
/// <param name="Fecha">Fecha del pago.</param>
/// <param name="Monto">Importe pagado.</param>
/// <param name="MetodoPago">Efectivo, Tarjeta o Transferencia.</param>
public sealed record PagoTratamientoDto(
    DateTime Fecha,
    decimal Monto,
    MetodoPago MetodoPago);

/// <summary>
/// Datos completos de la pantalla "Detalle de tratamiento".
/// Incluye la información clínica y los pagos asociados.
/// </summary>
/// <param name="Id">Id del tratamiento realizado.</param>
/// <param name="Paciente">Nombre completo del paciente.</param>
/// <param name="Tratamiento">Nombre del tratamiento del catálogo.</param>
/// <param name="Odontologo">Nombre completo del odontólogo.</param>
/// <param name="Fecha">Fecha del tratamiento (UTC en datos, local en pantalla).</param>
/// <param name="PiezaDental">Número FDI, o null si no aplica.</param>
/// <param name="Precio">Precio total acordado.</param>
/// <param name="Estado">Estado clínico.</param>
/// <param name="Observaciones">Notas clínicas.</param>
/// <param name="Pagos">Lista de pagos registrados (solo lectura).</param>
public sealed record TratamientoDetalleDto(
    int Id,
    string Paciente,
    string Tratamiento,
    string Odontologo,
    DateTime Fecha,
    string? PiezaDental,
    decimal Precio,
    EstadoTratamiento Estado,
    string? Observaciones,
    IReadOnlyList<PagoTratamientoDto> Pagos)
{
    /// <summary>Suma de todos los pagos registrados.</summary>
    public decimal TotalPagado => Pagos.Sum(pago => pago.Monto);

    /// <summary>
    /// Lo que falta por pagar. Puede ser negativo si hay sobrepago;
    /// la UI lo muestra tal cual para que el usuario lo detecte.
    /// </summary>
    public decimal SaldoPendiente => Precio - TotalPagado;

    /// <summary>
    /// Estado financiero derivado (no se guarda en la BD):
    /// Pagado si el saldo es 0 o menor, Pago parcial si hay pagos,
    /// y Pendiente si todavía no se ha pagado nada.
    /// </summary>
    public string EstadoFinanciero => SaldoPendiente <= 0
        ? "Pagado"
        : TotalPagado <= 0
            ? "Pendiente"
            : "Pago parcial";
}
