using Duende.IdentityServer.Services;
using Holmes.Identity.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Holmes.Identity.Server.Pages.Account;

public class LogoutModel(
    SignInManager<ApplicationUser> signInManager,
    IIdentityServerInteractionService interaction,
    ILogger<LogoutModel> logger
) : PageModel
{
    public string? LogoutId { get; set; }
    public bool ShowLogoutPrompt { get; set; } = true;

    public async Task<IActionResult> OnGetAsync(string? logoutId)
    {
        LogoutId = logoutId;

        var context = await interaction.GetLogoutContextAsync(LogoutId);
        if (context?.ShowSignoutPrompt == false)
        {
            return await OnPostAsync(logoutId);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? logoutId)
    {
        LogoutId = logoutId;

        if (User.Identity?.IsAuthenticated == true)
        {
            await signInManager.SignOutAsync();
            logger.LogInformation("User logged out");
        }

        var context = await interaction.GetLogoutContextAsync(LogoutId);

        if (!string.IsNullOrEmpty(context?.PostLogoutRedirectUri))
        {
            return Redirect(context.PostLogoutRedirectUri);
        }

        return RedirectToPage("./LoggedOut", new { logoutId = LogoutId });
    }
}
