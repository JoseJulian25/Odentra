using Odentra.Services.Dashboard.DTOs;

namespace Odentra.Services.Dashboard;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}
