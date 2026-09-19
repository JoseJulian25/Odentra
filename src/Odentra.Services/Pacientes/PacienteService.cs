using Odentra.Data.Entities;
using Odentra.Data.Repositories;
using Odentra.Services.Pacientes.DTOs;

namespace Odentra.Services.Pacientes;

public sealed class PacienteService(IPacienteRepository repository) : IPacienteService
{
    public async Task<IReadOnlyList<PacienteListItemDto>> SearchAsync(
        string? searchTerm,
        EstadoRegistro? estado,
        CancellationToken cancellationToken = default)
    {
        var pacientes = await repository.SearchAsync(searchTerm, estado, cancellationToken);

        return pacientes
            .Select(patient => new PacienteListItemDto(
                patient.Id,
                $"{patient.Nombres} {patient.Apellidos}",
                patient.Identificacion,
                patient.Telefono,
                patient.Correo,
                patient.FechaRegistro,
                patient.Estado))
            .ToList();
    }

    public async Task<PacienteDetalleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var patient = await repository.GetByIdAsync(id, cancellationToken);
        return patient is null ? null : ToDetail(patient);
    }

    public async Task<PacienteFormDto?> GetFormAsync(int id, CancellationToken cancellationToken = default)
    {
        var patient = await repository.GetByIdAsync(id, cancellationToken);
        return patient is null ? null : ToForm(patient);
    }

    public async Task<int> CreateAsync(PacienteFormDto model, CancellationToken cancellationToken = default)
    {
        var normalizedIdentification = Normalize(model.Identificacion);
        await EnsureIdentificationIsAvailableAsync(normalizedIdentification, null, cancellationToken);

        var patient = new Paciente();
        ApplyForm(patient, model, normalizedIdentification);
        patient.FechaRegistro = DateTime.UtcNow;
        patient.Estado = EstadoRegistro.Activo;

        await repository.AddAsync(patient, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return patient.Id;
    }

    public async Task UpdateAsync(PacienteFormDto model, CancellationToken cancellationToken = default)
    {
        var patient = await repository.GetByIdAsync(model.Id, cancellationToken)
            ?? throw new InvalidOperationException("El paciente no existe.");

        var normalizedIdentification = Normalize(model.Identificacion);
        await EnsureIdentificationIsAvailableAsync(normalizedIdentification, model.Id, cancellationToken);

        ApplyForm(patient, model, normalizedIdentification);
        patient.Estado = model.Estado;
        await repository.SaveChangesAsync(cancellationToken);
    }

    public async Task SetStatusAsync(int id, EstadoRegistro estado, CancellationToken cancellationToken = default)
    {
        var patient = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("El paciente no existe.");

        patient.Estado = estado;
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureIdentificationIsAvailableAsync(string? identification, int? excludedId, CancellationToken cancellationToken)
    {
        if (identification is not null && await repository.ExistsWithIdentificationAsync(identification, excludedId, cancellationToken))
        {
            throw new InvalidOperationException("La identificación ya está registrada para otro paciente.");
        }
    }

    private static void ApplyForm(Paciente patient, PacienteFormDto model, string? identification)
    {
        patient.Nombres = model.Nombres.Trim();
        patient.Apellidos = model.Apellidos.Trim();
        patient.Identificacion = identification;
        patient.FechaNacimiento = model.FechaNacimiento;
        patient.Sexo = Normalize(model.Sexo);
        patient.Telefono = Normalize(model.Telefono);
        patient.Correo = Normalize(model.Correo);
        patient.Direccion = Normalize(model.Direccion);
        patient.ContactoEmergenciaNombre = Normalize(model.ContactoEmergenciaNombre);
        patient.ContactoEmergenciaTelefono = Normalize(model.ContactoEmergenciaTelefono);
        patient.ContactoEmergenciaRelacion = Normalize(model.ContactoEmergenciaRelacion);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PacienteFormDto ToForm(Paciente patient) => new()
    {
        Id = patient.Id,
        Estado = patient.Estado,
        Nombres = patient.Nombres,
        Apellidos = patient.Apellidos,
        Identificacion = patient.Identificacion,
        FechaNacimiento = patient.FechaNacimiento,
        Sexo = patient.Sexo,
        Telefono = patient.Telefono,
        Correo = patient.Correo,
        Direccion = patient.Direccion,
        ContactoEmergenciaNombre = patient.ContactoEmergenciaNombre,
        ContactoEmergenciaTelefono = patient.ContactoEmergenciaTelefono,
        ContactoEmergenciaRelacion = patient.ContactoEmergenciaRelacion
    };

    private static PacienteDetalleDto ToDetail(Paciente patient) => new(
        patient.Id,
        patient.Nombres,
        patient.Apellidos,
        patient.Identificacion,
        patient.FechaNacimiento,
        patient.Sexo,
        patient.Telefono,
        patient.Correo,
        patient.Direccion,
        patient.ContactoEmergenciaNombre,
        patient.ContactoEmergenciaTelefono,
        patient.ContactoEmergenciaRelacion,
        patient.FechaRegistro,
        patient.Estado);
}