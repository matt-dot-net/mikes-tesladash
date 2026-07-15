using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeslaDash.Services;

namespace TeslaDash.Pages.Auth;

public sealed class CallbackModel(TeslaOAuthClient oauthClient) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string? code, string? state, string? error, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(error)) return RedirectToPage("/Connect", new { authError = error });
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state) ||
            !Request.Cookies.TryGetValue("tesla_oauth_state", out var expectedState) ||
            !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(state), System.Text.Encoding.UTF8.GetBytes(expectedState)))
            return BadRequest("Invalid or expired Tesla OAuth state.");

        Response.Cookies.Delete("tesla_oauth_state");
        await oauthClient.ExchangeCodeAsync(code, cancellationToken);
        return RedirectToPage("/Connect");
    }
}
