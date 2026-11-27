using System.ComponentModel.DataAnnotations;
using Odentra.Data.Entities;

namespace Odentra.Services.Tratamientos.DTOs;

/// <summary>
/// Modelo del formulario para registrar un tratamiento realizado.
/// Las claves foráneas usan 0 como "sin seleccionar" para que [Range] las marque como inválidas.
/// </summary>
public sealed class TratamientoRealizadoFormDto
{
    /// <summary>Id del registro. El formulario actual solo crea, pero se conserva para futuras ediciones.</summary>
    public int Id { get; set; }

    /// <summary>Paciente al que se aplica el tratamiento.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un paciente.")]
    public int PacienteId { get; set; }

    /// <summary>Tratamiento del catálogo que se aplicó.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un tratamiento.")]
    public int TratamientoId { get; set; }

    /// <summary>Odontólogo que realizó el tratamiento.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un odontólogo.")]
    public int OdontologoId { get; set; }

    /// <summary>Pieza dental opcional (null = tratamiento general, sin pieza).</summary>
    public int? PiezaDentalId { get; set; }

    /// <summary>Fecha del tratamiento. Se inicializa con la fecha local de hoy.</summary>
    [Required(ErrorMessage = "La fecha es obligatoria.")]
    public DateTime Fecha { get; set; } = DateTime.Today;

    /// <summary>
    /// Precio final cobrado. Se autocompleta con el precio base del catálogo,
    /// pero el usuario puede ajustarlo (descuentos, variaciones, etc.).
    /// </summary>
    [Range(typeof(decimal), "0.01", "9999999.99", ErrorMessage = "El precio debe ser mayor que 0.")]
    public decimal Precio { get; set; }

    /// <summary>Estado clínico inicial del tratamiento.</summary>
    public EstadoTratamiento Estado { get; set; } = EstadoTratamiento.Pendiente;

    /// <summary>Notas clínicas opcionales (máximo 1000 caracteres).</summary>
    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    public string? Observaciones { get; set; }
}
