using Microsoft.AspNetCore.Mvc.RazorPages;
using TeslaDash.Models;
using TeslaDash.Services;

namespace TeslaDash.Pages;

public class IndexModel(IVehicleDashboardService dashboardService) : PageModel
{
    public DashboardSnapshot Snapshot { get; private set; } = default!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Snapshot = await dashboardService.GetSnapshotAsync(cancellationToken);
    }
}
