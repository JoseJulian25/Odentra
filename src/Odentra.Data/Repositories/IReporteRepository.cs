using Odentra.Data.Entities;

namespace Odentra.Data.Repositories;

/// <summary>
/// Consultas de solo lectura para el módulo de Reportes. No modifica datos:
/// solo trae las entidades que el servicio necesita para calcular los resúmenes
/// y armar las filas de cada reporte.
/// </summary>
public interface IReporteRepository
{
    /// <summary>Citas dentro del rango de fechas, con paciente y odontólogo.</summary>
    Task<IReadOnlyList<Cita>> GetCitasAsync(
        DateOnly? desde,
        DateOnly? hasta,
        int? odontologoId,
        EstadoCita? estado,
        CancellationToken cancellationToken = default);

    /// <summary>Pacientes registrados dentro del rango de fechas.</summary>
    Task<IReadOnlyList<Paciente>> GetPacientesAsync(
        DateTime? desde,
        DateTime? hasta,
        EstadoRegistro? estado,
        CancellationToken cancellationToken = default);

    /// <summary>Tratamientos realizados dentro del rango de fechas.</summary>
    Task<IReadOnlyList<TratamientoRealizado>> GetTratamientosAsync(
        DateTime? desde,
        DateTime? hasta,
        int? odontologoId,
        EstadoTratamiento? estado,
        CancellationToken cancellationToken = default);

    /// <summary>Pagos registrados dentro del rango de fechas.</summary>
    Task<IReadOnlyList<Pago>> GetPagosAsync(
        DateTime? desde,
        DateTime? hasta,
        MetodoPago? metodo,
        CancellationToken cancellationToken = default);

    /// <summary>Odontólogos disponibles para el filtro del reporte.</summary>
    Task<IReadOnlyList<Odontologo>> GetOdontologosAsync(
        CancellationToken cancellationToken = default);
}
