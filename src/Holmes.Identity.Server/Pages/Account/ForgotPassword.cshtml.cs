using System.ComponentModel.DataAnnotations;
using System.Text;
using Holmes.Identity.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Holmes.Identity.Server.Pages.Account;

public class ForgotPasswordModel(
    UserManager<ApplicationUser> userManager,
    IOptions<ProvisioningOptions> provisioningOptions,
    ILogger<ForgotPasswordModel> logger
) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RequestSent { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null || !await userManager.IsEmailConfirmedAsync(user))
        {
            RequestSent = true;
            return Page();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        var baseUrl = provisioningOptions.Value.BaseUrl ?? Request.Scheme + "://" + Request.Host;
        var resetLink = $"{baseUrl}/Account/ResetPassword?email={Uri.EscapeDataString(Input.Email)}&code={encodedToken}";

        logger.LogInformation("Password reset requested for user {UserId}", user.Id);

        RequestSent = true;
        return Page();
    }
}
