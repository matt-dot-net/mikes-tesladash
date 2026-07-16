using TeslaDash.Models;

namespace TeslaDash.Services;

public sealed class TeslaVehicleDashboardService(TeslaFleetClient fleetClient) : IVehicleDashboardService
{
    public async Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var vehicles = await fleetClient.ListVehiclesAsync(cancellationToken);
        var vehicle = vehicles.FirstOrDefault()
            ?? throw new InvalidOperationException("No vehicles are authorized for this Tesla account.");

        var data = await fleetClient.GetVehicleDataAsync(vehicle.Id, cancellationToken);
        var charge = data.ChargeState;
        var vehicleState = data.VehicleState;
        var isCharging = string.Equals(charge?.ChargingState, "Charging", StringComparison.OrdinalIgnoreCase);

        return new DashboardSnapshot(
            string.IsNullOrWhiteSpace(data.DisplayName) ? vehicle.DisplayName : data.DisplayName,
            isCharging ? "Charging" : FormatState(data.State),
            charge?.BatteryLevel ?? 0,
            charge?.ChargeLimitSoc ?? 0,
            (int)Math.Round(charge?.BatteryRange ?? 0),
            vehicleState?.CarVersion?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Unknown",
            "Fleet Telemetry not configured",
            (decimal)(vehicleState?.Odometer ?? 0),
            0,
            isCharging
                ? new ChargeSession(
                    (decimal)(charge?.ChargeEnergyAdded ?? 0),
                    charge?.ChargerPower ?? 0,
                    charge?.MinutesToFullCharge ?? 0,
                    0)
                : null,
            []);
    }

    private static string FormatState(string state) => state.ToLowerInvariant() switch
    {
        "online" => "Online",
        "asleep" => "Asleep",
        "offline" => "Offline",
        _ => state
    };
}
