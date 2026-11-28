using Odentra.Services.Pagos;

namespace Odentra.UI.Components;

/// <summary>
/// Texto y clase CSS del estado de pago de un tratamiento.
/// Se usa en el módulo de Tratamientos para que la etiqueta sea consistente.
/// </summary>
public static class EstadoFinancieroUi
{
    public static string Texto(EstadoFinanciero estado) => estado switch
    {
        EstadoFinanciero.Pagado => "Pagado",
        EstadoFinanciero.PagoParcial => "Pago parcial",
        _ => "Pendiente"
    };

    public static string Clase(EstadoFinanciero estado) => estado switch
    {
        EstadoFinanciero.Pagado => "estado-pagado",
        EstadoFinanciero.PagoParcial => "estado-parcial",
        _ => "estado-pendiente"
    };
}
