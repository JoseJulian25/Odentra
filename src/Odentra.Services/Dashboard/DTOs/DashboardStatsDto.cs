namespace Odentra.Services.Dashboard.DTOs;

public class ActividadRecienteDto
{
    public string Accion { get; set; } = string.Empty; // "Creación", "Modificación"
    public string Entidad { get; set; } = string.Empty; // "Paciente", "Cita", "Odontologo"
    public string Mensaje { get; set; } = string.Empty; // "Se registró al paciente Juan Pérez"
    public DateTime Fecha { get; set; }
    public string Usuario { get; set; } = "Sistema";

    // Propiedades calculadas para la interfaz visual que ya diseñaste
    public string Icono => Entidad switch { "Cita" => "✓", "Paciente" => "+", _ => "¤" };
    public string ClaseIcono => Entidad switch { "Cita" => "success", "Paciente" => "info", _ => "payment" };
}

public sealed record DashboardStatsDto(
    int TotalPacientesActivos,
    int TotalOdontologosActivos,
    int PacientesRegistradosEstaSemana,
    int OdontologosRegistradosEstaSemana);
