using Odentra.Data.Entities;

namespace Odentra.Services.Auditoria;

/// <summary>
/// Traduce los nombres técnicos de las entidades a textos legibles para la
/// pantalla de auditoría (por ejemplo, RegistroOdontograma → Pieza dental).
/// </summary>
public static class AuditoriaEtiquetas
{
    /// <summary>
    /// Entidades conocidas, en el orden en que se muestran en el filtro.
    /// Las que aparezcan en la base pero no estén aquí se agregan al final.
    /// </summary>
    public static readonly IReadOnlyList<string> Conocidas =
    [
        nameof(Paciente),
        nameof(Odontologo),
        nameof(Cita),
        nameof(HistoriaClinica),
        nameof(ConsultaClinica),
        nameof(RegistroOdontograma),
        nameof(Tratamiento),
        nameof(TratamientoRealizado),
        nameof(Pago),
        nameof(Usuario),
        nameof(Rol),
        nameof(Permiso),
        nameof(RolPermiso),
        "Sesión"
    ];

    public static string Entidad(string? entidad) => entidad switch
    {
        nameof(Paciente) => "Paciente",
        nameof(Odontologo) => "Odontólogo",
        nameof(Cita) => "Cita",
        nameof(HistoriaClinica) => "Historia clínica",
        nameof(ConsultaClinica) => "Consulta clínica",
        nameof(RegistroOdontograma) => "Pieza dental",
        nameof(Tratamiento) => "Tratamiento",
        nameof(TratamientoRealizado) => "Tratamiento realizado",
        nameof(Pago) => "Pago",
        nameof(Usuario) => "Usuario",
        nameof(Rol) => "Rol",
        nameof(Permiso) => "Permiso",
        nameof(RolPermiso) => "Permiso de rol",
        "Sesión" => "Sesión",
        _ => string.IsNullOrWhiteSpace(entidad) ? "—" : entidad
    };
}
