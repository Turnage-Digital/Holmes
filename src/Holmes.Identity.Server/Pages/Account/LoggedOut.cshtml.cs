using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Holmes.Identity.Server.Pages.Account;

[AllowAnonymous]
public class LoggedOutModel(
    IIdentityServerInteractionService interaction
) : PageModel
{
    public string? PostLogoutRedirectUri { get; set; }
    public string? ClientName { get; set; }
    public string? SignOutIframeUrl { get; set; }
    public bool AutomaticRedirectAfterSignOut { get; set; }

    public async Task<IActionResult> OnGetAsync(string? logoutId)
    {
        var context = await interaction.GetLogoutContextAsync(logoutId);

        PostLogoutRedirectUri = context?.PostLogoutRedirectUri;
        ClientName = context?.ClientName ?? context?.ClientId;
        SignOutIframeUrl = context?.SignOutIFrameUrl;
        AutomaticRedirectAfterSignOut = !string.IsNullOrEmpty(PostLogoutRedirectUri);

        return Page();
    }
}
