using System.ComponentModel.DataAnnotations;
using System.Text;
using Holmes.Identity.Server.Data;
using Holmes.Identity.Server.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Holmes.Identity.Server.Pages.Account;

public class ResetPasswordModel(
    UserManager<ApplicationUser> userManager,
    IPasswordHistoryService passwordHistoryService,
    TimeProvider timeProvider,
    IOptions<PasswordPolicyOptions> passwordOptions,
    ILogger<ResetPasswordModel> logger
) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool ResetComplete { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;
    }

    public IActionResult OnGet(string? email, string? code)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
        {
            return RedirectToPage("./Login");
        }

        Input = new InputModel
        {
            Email = email,
            Code = code
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            ResetComplete = true;
            return Page();
        }

        var oldHash = user.PasswordHash;
        var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
        var result = await userManager.ResetPasswordAsync(user, decodedCode, Input.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        var now = timeProvider.GetUtcNow();
        var expirationDays = passwordOptions.Value.ExpirationDays;
        user.LastPasswordChangedAt = now;
        user.PasswordExpires = now.AddDays(expirationDays);
        await userManager.UpdateAsync(user);

        if (!string.IsNullOrEmpty(oldHash))
        {
            await passwordHistoryService.RecordPasswordChangeAsync(user.Id, oldHash);
        }

        logger.LogInformation("User {Email} reset their password", Input.Email);

        ResetComplete = true;
        return Page();
    }
}
