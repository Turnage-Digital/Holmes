using System.ComponentModel.DataAnnotations;
using Duende.IdentityServer.Services;
using Holmes.Identity.Server.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Holmes.Identity.Server.Pages.Account;

public class LoginModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    IIdentityServerInteractionService interaction,
    TimeProvider timeProvider,
    ILogger<LoginModel> logger
) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await signInManager.PasswordSignInAsync(
            Input.Email,
            Input.Password,
            Input.RememberMe,
            true);

        if (result.Succeeded)
        {
            var user = await userManager.FindByEmailAsync(Input.Email);
            if (user?.PasswordExpires is not null &&
                user.PasswordExpires.Value < timeProvider.GetUtcNow())
            {
                logger.LogInformation("User {Email} has expired password, redirecting to change password", Input.Email);
                await signInManager.SignOutAsync();
                return RedirectToPage("./ChangePassword", new { expired = true, returnUrl });
            }

            logger.LogInformation("User {Email} logged in", Input.Email);

            var context = await interaction.GetAuthorizationContextAsync(returnUrl);
            if (context is not null)
            {
                return Redirect(returnUrl);
            }

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return Redirect("~/");
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, Input.RememberMe });
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User {Email} account locked out", Input.Email);
            ModelState.AddModelError(string.Empty, "Account locked out. Please try again later.");
            return Page();
        }

        if (result.IsNotAllowed)
        {
            ModelState.AddModelError(string.Empty, "You must confirm your email before logging in.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return Page();
    }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}