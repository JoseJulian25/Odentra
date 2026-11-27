using System.ComponentModel.DataAnnotations;
using Odentra.Data.Entities;

namespace Odentra.Services.Tratamientos.DTOs;

/// <summary>
/// Modelo del formulario para crear o editar un tratamiento del catálogo.
/// Es una clase (no record) porque el formulario Blazor enlaza y modifica sus propiedades.
/// Los atributos de DataAnnotations se validan tanto en el EditForm como en el servicio.
/// </summary>
public sealed class TratamientoFormDto
{
    /// <summary>Id del tratamiento. Vale 0 cuando es un registro nuevo.</summary>
    public int Id { get; set; }

    /// <summary>Nombre obligatorio, entre 3 y 150 caracteres.</summary>
    [Required(ErrorMessage = "El nombre del tratamiento es obligatorio.")]
    [StringLength(150, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Descripción opcional; la base de datos admite texto largo.</summary>
    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string? Descripcion { get; set; }

    /// <summary>
    /// Precio base. Se valida con decimal para evitar problemas de precisión.
    /// Debe ser mayor que 0 y respetar la precisión monetaria (2 decimales en BD).
    /// </summary>
    [Range(typeof(decimal), "0.01", "9999999.99", ErrorMessage = "El precio base debe ser mayor que 0.")]
    public decimal PrecioBase { get; set; }

    /// <summary>Estado del catálogo. Los inactivos no se pueden usar en registros nuevos.</summary>
    public EstadoRegistro Estado { get; set; } = EstadoRegistro.Activo;
}
