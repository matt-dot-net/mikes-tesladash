namespace TeslaDash.Models;

public sealed record DashboardSnapshot(string VehicleName, string ConnectionState, int StateOfCharge,
    int ChargeLimit, int EstimatedRangeMiles, string SoftwareVersion, string FsdVersion,
    decimal MilesDriven, decimal FsdMiles, ChargeSession? ActiveCharge, IReadOnlyList<ActivityEvent> RecentEvents,
    double? Latitude = null, double? Longitude = null)
{
    public decimal FsdPercent => MilesDriven <= 0 ? 0 : Math.Round(FsdMiles / MilesDriven * 100, 1);
}

public sealed record ChargeSession(decimal AddedKwh, decimal RateKw, int MinutesRemaining, decimal EstimatedCost);
public sealed record ActivityEvent(DateTimeOffset OccurredAt, string Kind, string Message);
