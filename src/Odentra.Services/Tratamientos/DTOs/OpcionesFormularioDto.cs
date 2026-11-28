namespace Odentra.Services.Tratamientos.DTOs;

/// <summary>Paciente para el selector del formulario de tratamientos realizados.</summary>
public sealed record PacienteOptionDto(int Id, string Nombre, bool Activo);

/// <summary>Odontólogo para el selector del formulario de tratamientos realizados.</summary>
public sealed record OdontologoOptionDto(int Id, string Nombre, bool Activo);

/// <summary>Pieza dental para el selector del formulario de tratamientos realizados.</summary>
public sealed record PiezaDentalOptionDto(int Id, string NumeroFDI, string Nombre);

/// <summary>Consulta clínica del paciente, para asociarla opcionalmente al tratamiento.</summary>
public sealed record ConsultaOptionDto(int Id, DateTime Fecha, string Motivo);

/// <summary>Tratamiento del catálogo para el selector del formulario de realizados.</summary>
public sealed record TratamientoOptionDto(int Id, string Nombre, decimal PrecioBase, bool Activo);
