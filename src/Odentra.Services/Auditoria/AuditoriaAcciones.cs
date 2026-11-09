namespace Odentra.Services.Auditoria;

/// <summary>
/// Nivel visual de un evento de auditoría. Se deriva de la acción para que la
/// interfaz pinte la etiqueta correcta sin conocer los textos internos.
/// </summary>
public enum NivelAuditoria
{
    Neutro,
    Exito,
    Informativo,
    Advertencia,
    Critico
}

/// <summary>
/// Catálogo de acciones auditables y la forma de clasificarlas.
/// Los textos son los mismos que ya escriben los distintos servicios.
/// </summary>
public static class AuditoriaAcciones
{
    public const string InicioSesion = "Inició sesión";
    public const string CierreSesion = "Cerró sesión";
    public const string InicioSesionFallido = "Falló inicio de sesión";

    /// <summary>
    /// Orden preferido para el filtro de acciones. Las acciones nuevas que
    /// aparezcan en la base se agregan al final automáticamente.
    /// </summary>
    public static readonly IReadOnlyList<string> Conocidas =
    [
        "Agregó",
        "Creó",
        "Agendó",
        "Registró",
        "Modificó",
        "Cambió",
        "Activó",
        "Desactivó",
        "Canceló",
        "Cerró",
        "Restableció",
        "Eliminó",
        InicioSesion,
        CierreSesion,
        InicioSesionFallido
    ];

    public static NivelAuditoria ObtenerNivel(string? accion) => accion switch
    {
        "Agregó" or "Creó" or "Agendó" or "Registró" or "Activó" => NivelAuditoria.Exito,
        "Modificó" or "Cambió" or "Cerró" => NivelAuditoria.Informativo,
        "Desactivó" or "Canceló" or "Restableció" => NivelAuditoria.Advertencia,
        "Eliminó" or InicioSesionFallido => NivelAuditoria.Critico,
        InicioSesion or CierreSesion => NivelAuditoria.Neutro,
        _ => NivelAuditoria.Informativo
    };
}

/// <summary>
/// Módulos conocidos, en el orden en que aparecen en el sistema. Se usa para
/// que el filtro de módulos tenga opciones incluso antes del primer registro.
/// </summary>
public static class AuditoriaModulos
{
    public static readonly IReadOnlyList<string> Conocidos =
    [
        "Pacientes",
        "Odontólogos",
        "Citas",
        "Historia Clínica",
        "Odontograma",
        "Tratamientos",
        "Pagos",
        "Usuarios",
        "Roles y Permisos",
        "Sistema"
    ];
}
