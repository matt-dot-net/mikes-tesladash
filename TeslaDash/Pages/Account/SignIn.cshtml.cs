using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TeslaDash.Pages.Account;

[AllowAnonymous]
public sealed class SignInModel : PageModel
{
    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(SafeReturnUrl(returnUrl));
        }

        return Challenge(
            new AuthenticationProperties { RedirectUri = SafeReturnUrl(returnUrl) },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    private string SafeReturnUrl(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Page("/Index")!;
}
