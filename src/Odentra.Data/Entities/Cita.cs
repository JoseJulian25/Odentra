namespace Odentra.Data.Entities;

public class Cita
{
    public int Id { get; set; }
    public int PacienteId { get; set; }
    public int OdontologoId { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }
    public string? Motivo { get; set; }
    public string? Observaciones { get; set; }
    public EstadoCita Estado { get; set; } = EstadoCita.Programada;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    public Paciente Paciente { get; set; } = null!;
    public Odontologo Odontologo { get; set; } = null!;
    public ConsultaClinica? ConsultaClinica { get; set; }
}