using TeslaDash.Models;

namespace TeslaDash.Services;

public interface IVehicleDashboardService
{
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
