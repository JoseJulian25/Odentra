using Odentra.Data.Entities;

namespace Odentra.Services.Tratamientos.DTOs;

/// <summary>
/// Fila del catálogo de tratamientos que consume la tabla de la UI.
/// Es un record: inmutable y pensado solo para transportar datos ya proyectados.
/// </summary>
/// <param name="Id">Identificador del tratamiento.</param>
/// <param name="Nombre">Nombre visible en el catálogo.</param>
/// <param name="Descripcion">Descripción opcional.</param>
/// <param name="PrecioBase">Precio de referencia del catálogo.</param>
/// <param name="Estado">Activo o Inactivo.</param>
public sealed record TratamientoListItemDto(
    int Id,
    string Nombre,
    string? Descripcion,
    decimal PrecioBase,
    EstadoRegistro Estado);
