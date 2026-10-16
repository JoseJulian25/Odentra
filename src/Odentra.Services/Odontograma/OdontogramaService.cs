using Odentra.Data.Entities;
using Odentra.Data.Repositories;
using Odentra.Services.Odontograma.DTOs;

namespace Odentra.Services.Odontograma;

public sealed class OdontogramaService(IOdontogramaRepository repository) : IOdontogramaService
{
    public async Task<IReadOnlyList<OdontogramaPacienteDto>> GetActivePatientsAsync(CancellationToken cancellationToken = default)
    {
        var patients = await repository.GetActivePatientsAsync(cancellationToken);
        return patients.Select(patient => new OdontogramaPacienteDto(patient.Id, $"{patient.Nombres} {patient.Apellidos}")).ToList();
    }

    public async Task<IReadOnlyList<OdontogramaPiezaDto>> GetByPatientAsync(int pacienteId, CancellationToken cancellationToken = default)
    {
        var pieces = await repository.GetPiecesAsync(cancellationToken);
        var records = (await repository.GetRecordsAsync(pacienteId, cancellationToken)).ToDictionary(record => record.PiezaDentalId);

        return pieces.Select(piece => records.TryGetValue(piece.Id, out var record)
            ? new OdontogramaPiezaDto
            {
                Id = piece.Id,
                NumeroFDI = piece.NumeroFDI,
                Nombre = piece.Nombre,
                Estado = record.Estado,
                Observaciones = record.Observaciones,
                FechaModificacion = record.FechaModificacion,
                Odontologo = $"{record.Odontologo.Nombres} {record.Odontologo.Apellidos}"
            }
            : new OdontogramaPiezaDto { Id = piece.Id, NumeroFDI = piece.NumeroFDI, Nombre = piece.Nombre })
            .ToList();
    }

    public async Task SaveAsync(int pacienteId, int piezaDentalId, EstadoOdontograma estado, string? observaciones, string userId, CancellationToken cancellationToken = default)
    {
        var odontologoId = await repository.GetOdontologoIdByUserAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("El usuario actual no está asociado a un odontólogo.");
        var record = await repository.GetRecordAsync(pacienteId, piezaDentalId, cancellationToken);

        if (record is null)
        {
            record = new RegistroOdontograma
            {
                PacienteId = pacienteId,
                PiezaDentalId = piezaDentalId,
                OdontologoId = odontologoId
            };
            await repository.AddAsync(record, cancellationToken);
        }

        record.Estado = estado;
        record.Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
        record.OdontologoId = odontologoId;
        record.FechaModificacion = DateTime.UtcNow;
        await repository.SaveChangesWithAuditAsync(new RegistroAuditoria
        {
            UsuarioId = userId,
            Accion = "EDITAR",
            Modulo = "Odontograma",
            Entidad = nameof(RegistroOdontograma),
            EntidadId = record.Id,
            Descripcion = $"Se actualizó la pieza dental {piezaDentalId} del paciente {pacienteId}."
        }, cancellationToken);
    }
}