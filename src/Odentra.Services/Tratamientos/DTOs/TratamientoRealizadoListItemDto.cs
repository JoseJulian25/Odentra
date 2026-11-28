using Odentra.Data.Entities;
using Odentra.Services.Pagos;

namespace Odentra.Services.Tratamientos.DTOs;

/// <summary>
/// Fila de la tabla de tratamientos realizados.
/// Incluye nombres ya resueltos y el estado financiero calculado a partir de los pagos.
/// </summary>
/// <param name="Id">Id del tratamiento realizado (se usa para abrir el detalle).</param>
/// <param name="PacienteId">Id del paciente (para filtrar o navegar a su ficha).</param>
/// <param name="Paciente">Nombre completo del paciente.</param>
/// <param name="Tratamiento">Nombre del tratamiento del catálogo.</param>
/// <param name="Odontologo">Nombre completo del odontólogo.</param>
/// <param name="Fecha">Fecha del tratamiento (se muestra en hora local).</param>
/// <param name="PiezaDental">Número FDI de la pieza, o null si es general.</param>
/// <param name="Precio">Precio cobrado en este registro.</param>
/// <param name="Pagado">Suma de los pagos registrados.</param>
/// <param name="Saldo">Precio menos lo pagado.</param>
/// <param name="EstadoPago">Pagado, Pago parcial o Pendiente.</param>
/// <param name="Estado">Estado clínico del tratamiento.</param>
public sealed record TratamientoRealizadoListItemDto(
    int Id,
    int PacienteId,
    string Paciente,
    string Tratamiento,
    string Odontologo,
    DateTime Fecha,
    string? PiezaDental,
    decimal Precio,
    decimal Pagado,
    decimal Saldo,
    EstadoFinanciero EstadoPago,
    EstadoTratamiento Estado);
