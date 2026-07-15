using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using TeslaDash.Services;

namespace TeslaDash.Pages;

public sealed class ConnectModel(IOptions<TeslaOptions> options, ITeslaTokenStore tokenStore, TeslaFleetClient fleetClient, TeslaOAuthClient oauthClient) : PageModel
{
    public TeslaOptions Options { get; } = options.Value;
    public bool IsConnected { get; private set; }
    public IReadOnlyList<TeslaVehicle> Vehicles { get; private set; } = [];
    public string? Error { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        IsConnected = await tokenStore.GetAsync(cancellationToken) is not null;
        if (!IsConnected) return;
        try { Vehicles = await fleetClient.ListVehiclesAsync(cancellationToken); }
        catch (Exception ex) { Error = ex.Message; }
    }

    public IActionResult OnPostConnect()
    {
        if (!Options.IsConfigured) return RedirectToPage();
        var state = TeslaOAuthClient.CreateState();
        Response.Cookies.Append("tesla_oauth_state", state, new CookieOptions
        {
            HttpOnly = true, SameSite = SameSiteMode.Lax, Secure = Request.IsHttps,
            MaxAge = TimeSpan.FromMinutes(10), IsEssential = true
        });
        return Redirect(oauthClient.CreateAuthorizationUrl(state));
    }

    public async Task<IActionResult> OnPostDisconnectAsync(CancellationToken cancellationToken)
    {
        await tokenStore.ClearAsync(cancellationToken);
        return RedirectToPage();
    }
}
