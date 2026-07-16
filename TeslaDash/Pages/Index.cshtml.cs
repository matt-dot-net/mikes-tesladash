using Microsoft.AspNetCore.Mvc.RazorPages;
using TeslaDash.Models;
using TeslaDash.Services;

namespace TeslaDash.Pages;

public class IndexModel(IVehicleDashboardService dashboardService) : PageModel
{
    public DashboardSnapshot? Snapshot { get; private set; }
    public string? Error { get; private set; }

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
}
