namespace Odentra.Services.Tratamientos.DTOs;

/// <summary>
/// Opción genérica para un selector (paciente, odontólogo, pieza o tratamiento).
/// El texto ya viene formateado para mostrarse tal cual.
/// </summary>
/// <param name="Id">Valor que se guarda al seleccionar la opción.</param>
/// <param name="Texto">Texto visible en el select.</param>
/// <param name="Precio">
/// Precio base cuando la opción es un tratamiento; null para las demás listas.
/// Se usa para autocompletar el campo Precio del formulario.
/// </param>
public sealed record OpcionTratamientoDto(
    int Id,
    string Texto,
    decimal? Precio = null);

/// <summary>
/// Agrupa todas las listas que necesita el formulario de "Registrar tratamiento".
/// Se cargan en una sola llamada para simplificar la UI.
/// </summary>
/// <param name="Pacientes">Pacientes activos.</param>
/// <param name="Odontologos">Odontólogos activos.</param>
/// <param name="Piezas">Catálogo de piezas dentales.</param>
/// <param name="Tratamientos">Tratamientos activos con su precio base.</param>
public sealed record CatalogosRealizadoDto(
    IReadOnlyList<OpcionTratamientoDto> Pacientes,
    IReadOnlyList<OpcionTratamientoDto> Odontologos,
    IReadOnlyList<OpcionTratamientoDto> Piezas,
    IReadOnlyList<OpcionTratamientoDto> Tratamientos)
{
    /// <summary>
    /// Instancia vacía para inicializar el estado del componente
    /// y evitar null checks antes de la primera carga.
    /// </summary>
    public static CatalogosRealizadoDto Vacio { get; } = new([], [], [], []);
}
