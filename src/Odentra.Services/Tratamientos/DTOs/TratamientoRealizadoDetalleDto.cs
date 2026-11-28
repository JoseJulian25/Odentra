using Odentra.Data.Entities;
using Odentra.Services.Pagos;

namespace Odentra.Services.Tratamientos.DTOs;

/// <summary>
/// Detalle completo de un tratamiento realizado, con los totales financieros
/// calculados a partir de sus pagos. Se muestra en el modal de la página de Tratamientos.
/// </summary>
public sealed record TratamientoRealizadoDetalleDto(
    int Id,
    int PacienteId,
    string Paciente,
    string Tratamiento,
    string Odontologo,
    DateTime Fecha,
    string? PiezaDental,
    decimal Precio,
    EstadoTratamiento Estado,
    string? Observaciones,
    decimal Pagado,
    decimal Saldo,
    EstadoFinanciero EstadoPago,
    int CantidadPagos);
