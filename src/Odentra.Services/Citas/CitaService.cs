using Odentra.Data.Entities;
using Odentra.Data.Repositories;
using Odentra.Services.Citas.DTOs;

namespace Odentra.Services.Citas;

public sealed class CitaService(
    ICitaRepository repository,
    IPacienteRepository pacienteRepository,
    IOdontologoRepository odontologoRepository) : ICitaService
{
    public async Task<IReadOnlyList<CitaListItemDto>> SearchAsync(
        DateOnly? fecha = null,
        int? pacienteId = null,
        int? odontologoId = null,
        EstadoCita? estado = null,
        CancellationToken cancellationToken = default)
    {
        var citas = await repository.SearchAsync(fecha, pacienteId, odontologoId, estado, cancellationToken);

        return citas
            .Select(cita => new CitaListItemDto(
                cita.Id,
                $"{cita.Paciente.Nombres} {cita.Paciente.Apellidos}",
                $"{cita.Odontologo.Nombres} {cita.Odontologo.Apellidos}",
                cita.Fecha,
                cita.HoraInicio,
                cita.HoraFin,
                cita.Motivo,
                cita.Estado))
            .ToList();
    }

    public async Task<CitaDetalleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cita = await repository.GetByIdAsync(id, cancellationToken);
        return cita is null ? null : ToDetail(cita);
    }

    public async Task<CitaFormDto?> GetFormAsync(int id, CancellationToken cancellationToken = default)
    {
        var cita = await repository.GetByIdAsync(id, cancellationToken);
        return cita is null ? null : ToForm(cita);
    }

    public async Task<int> CreateAsync(CitaFormDto model, CancellationToken cancellationToken = default)
    {
        if (model.PacienteId <= 0)
            throw new InvalidOperationException("El ID del paciente no es válido.");

        if (model.OdontologoId <= 0)
            throw new InvalidOperationException("El ID del odontólogo no es válido.");

        if (model.Fecha == default)
            throw new InvalidOperationException("La fecha no es válida.");

        if (model.HoraInicio == default || model.HoraFin == default)
            throw new InvalidOperationException("Las horas no son válidas.");

        if (model.HoraFin <= model.HoraInicio)
            throw new InvalidOperationException("La hora de fin debe ser posterior a la hora de inicio.");

        var paciente = await pacienteRepository.GetByIdAsync(model.PacienteId, cancellationToken);
        if (paciente is null)
            throw new InvalidOperationException("El paciente con ID " + model.PacienteId + " no existe.");

        if (paciente.Estado != EstadoRegistro.Activo)
            throw new InvalidOperationException("El paciente debe estar activo para agendar citas.");

        var odontologo = await odontologoRepository.GetByIdAsync(model.OdontologoId, cancellationToken);
        if (odontologo is null)
            throw new InvalidOperationException("El odontólogo con ID " + model.OdontologoId + " no existe.");

        if (odontologo.Estado != EstadoRegistro.Activo)
            throw new InvalidOperationException("El odontólogo debe estar activo para agendar citas.");

        var cita = new Cita
        {
            PacienteId = model.PacienteId,
            OdontologoId = model.OdontologoId,
            Fecha = model.Fecha,
            HoraInicio = model.HoraInicio,
            HoraFin = model.HoraFin,
            Motivo = model.Motivo,
            Observaciones = model.Observaciones,
            Estado = EstadoCita.Programada
        };

        try
        {
            await repository.AddAsync(cita, cancellationToken);
            return cita.Id;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Error al guardar la cita en la base de datos: " + ex.Message, ex);
        }
    }

    public async Task UpdateAsync(CitaFormDto model, CancellationToken cancellationToken = default)
    {
        var cita = await repository.GetByIdAsync(model.Id, cancellationToken);
        if (cita is null)
            throw new InvalidOperationException("La cita no existe.");

        var paciente = await pacienteRepository.GetByIdAsync(model.PacienteId, cancellationToken);
        if (paciente is null)
            throw new InvalidOperationException("El paciente no existe.");

        var odontologo = await odontologoRepository.GetByIdAsync(model.OdontologoId, cancellationToken);
        if (odontologo is null)
            throw new InvalidOperationException("El odontólogo no existe.");

        if (model.HoraFin <= model.HoraInicio)
            throw new InvalidOperationException("La hora de fin debe ser posterior a la hora de inicio.");

        cita.PacienteId = model.PacienteId;
        cita.OdontologoId = model.OdontologoId;
        cita.Fecha = model.Fecha;
        cita.HoraInicio = model.HoraInicio;
        cita.HoraFin = model.HoraFin;
        cita.Motivo = model.Motivo;
        cita.Observaciones = model.Observaciones;
        cita.Estado = model.Estado;

        await repository.UpdateAsync(cita, cancellationToken);
    }

    public async Task SetEstadoAsync(int id, EstadoCita estado, CancellationToken cancellationToken = default)
    {
        var cita = await repository.GetByIdAsync(id, cancellationToken);
        if (cita is null)
            throw new InvalidOperationException("La cita no existe.");

        cita.Estado = estado;
        await repository.UpdateAsync(cita, cancellationToken);
    }

    private static CitaDetalleDto ToDetail(Cita cita) =>
        new(
            cita.Id,
            $"{cita.Paciente.Nombres} {cita.Paciente.Apellidos}",
            cita.Paciente.Id,
            $"{cita.Odontologo.Nombres} {cita.Odontologo.Apellidos}",
            cita.Odontologo.Id,
            cita.Fecha,
            cita.HoraInicio,
            cita.HoraFin,
            cita.Motivo,
            cita.Observaciones,
            cita.Estado,
            cita.FechaCreacion);

    private static CitaFormDto ToForm(Cita cita) =>
        new()
        {
            Id = cita.Id,
            PacienteId = cita.PacienteId,
            OdontologoId = cita.OdontologoId,
            Fecha = cita.Fecha,
            HoraInicio = cita.HoraInicio,
            HoraFin = cita.HoraFin,
            Motivo = cita.Motivo,
            Observaciones = cita.Observaciones,
            Estado = cita.Estado
        };
}
