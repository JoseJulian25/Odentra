using Odentra.Data.Entities;
using Odentra.Data.Repositories;
using Odentra.Services.Dashboard.DTOs;

namespace Odentra.Services.Dashboard;

public sealed class DashboardService(
    IPacienteRepository pacienteRepository,
    IOdontologoRepository odontologoRepository) : IDashboardService
{
    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var pacientesActivos = await pacienteRepository.SearchAsync(null, EstadoRegistro.Activo, cancellationToken);
        var odontologosActivos = await odontologoRepository.SearchAsync(null, EstadoRegistro.Activo, cancellationToken);

        var ahora = DateTime.Now;
        var inicioSemana = ahora.Subtract(TimeSpan.FromDays(ahora.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)ahora.DayOfWeek - 1));

        var pacientesEstaSemana = pacientesActivos.Count(p => p.FechaRegistro.Date >= inicioSemana.Date);

        return new DashboardStatsDto(
            TotalPacientesActivos: pacientesActivos.Count,
            TotalOdontologosActivos: odontologosActivos.Count,
            PacientesRegistradosEstaSemana: pacientesEstaSemana,
            OdontologosRegistradosEstaSemana: 0);
    }
}
