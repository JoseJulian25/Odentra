using Odentra.Data.Entities;

namespace Odentra.Services.Tratamientos.DTOs;

/// <summary>
/// Fila de la tabla de tratamientos realizados.
/// Incluye nombres ya resueltos para no obligar a la UI a navegar relaciones.
/// </summary>
/// <param name="Id">Id del tratamiento realizado (se usa para abrir el detalle).</param>
/// <param name="Paciente">Nombre completo del paciente.</param>
/// <param name="Tratamiento">Nombre del tratamiento del catálogo.</param>
/// <param name="Odontologo">Nombre completo del odontólogo.</param>
/// <param name="Fecha">Fecha del tratamiento (se muestra en hora local).</param>
/// <param name="PiezaDental">Número FDI de la pieza, o null si es general.</param>
/// <param name="Precio">Precio cobrado en este registro.</param>
/// <param name="Estado">Estado clínico del tratamiento.</param>
public sealed record TratamientoRealizadoListItemDto(
    int Id,
    string Paciente,
    string Tratamiento,
    string Odontologo,
    DateTime Fecha,
    string? PiezaDental,
    decimal Precio,
    EstadoTratamiento Estado);
