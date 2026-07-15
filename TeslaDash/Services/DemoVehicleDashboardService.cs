using TeslaDash.Models;

namespace TeslaDash.Services;

public sealed class DemoVehicleDashboardService : IVehicleDashboardService
{
    public Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.Now;
        return Task.FromResult(new DashboardSnapshot(
            "Quicksilver", "Charging at home", 68, 80, 196, "2026.20.3", "FSD (Supervised) 14.2.1",
            1_284.6m, 1_046.2m, new(18.4m, 10.9m, 42, 2.31m),
            [new(now.AddMinutes(-18), "charge", "Charging started at Home"),
             new(now.AddHours(-3), "drive", "Drive ended · 21.4 mi · 91% under FSD"),
             new(now.AddDays(-1), "software", "New software detected: 2026.20.3"),
             new(now.AddDays(-2), "status", "Vehicle left Home")]));
    }
}
