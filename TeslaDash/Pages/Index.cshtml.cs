using Microsoft.AspNetCore.Mvc.RazorPages;
using TeslaDash.Models;
using TeslaDash.Services;

namespace TeslaDash.Pages;

public class IndexModel(IVehicleDashboardService dashboardService, TeslaFleetClient fleetClient) : PageModel
{
    public DashboardSnapshot? Snapshot { get; private set; }
    public string? Error { get; private set; }
    [Microsoft.AspNetCore.Mvc.TempData]
    public string? CommandMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        try
        {
            Snapshot = await dashboardService.GetSnapshotAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }

    public async Task<Microsoft.AspNetCore.Mvc.IActionResult> OnPostFartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var vehicle = (await fleetClient.ListVehiclesAsync(cancellationToken)).FirstOrDefault()
                ?? throw new InvalidOperationException("No Tesla vehicle is authorized.");
            await fleetClient.RemoteBoomboxAsync(vehicle.Id, cancellationToken);
            CommandMessage = "Remote Boombox command sent.";
        }
        catch (Exception ex)
        {
            CommandMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
