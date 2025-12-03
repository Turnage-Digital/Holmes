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

public class ConfirmEmailModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IPasswordHistoryService passwordHistoryService,
    TimeProvider timeProvider,
    IOptions<PasswordPolicyOptions> passwordOptions,
    ILogger<ConfirmEmailModel> logger
) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }
    public string? UserId { get; set; }
    public string? Code { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool RequiresPassword { get; set; }
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? userId, string? code, string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
        {
            return RedirectToPage("/Index");
        }

        ReturnUrl = returnUrl ?? Url.Content("~/");
        UserId = userId;
        Code = code;

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            StatusMessage = "Unable to confirm email. The link may have expired.";
            return Page();
        }

        if (user.EmailConfirmed)
        {
            EmailConfirmed = true;
            StatusMessage = "Your email has already been confirmed.";
            return Page();
        }

        var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await userManager.ConfirmEmailAsync(user, decodedCode);

        if (!result.Succeeded)
        {
            StatusMessage = "Error confirming your email. The link may have expired.";
            return Page();
        }

        EmailConfirmed = true;
        RequiresPassword = string.IsNullOrEmpty(user.PasswordHash);

        if (RequiresPassword)
        {
            StatusMessage = "Email confirmed! Please set your password to complete your account setup.";
        }
        else
        {
            StatusMessage = "Thank you for confirming your email.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? userId, string? code, string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        UserId = userId;
        Code = code;

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Index");
        }

        if (!ModelState.IsValid)
        {
            EmailConfirmed = true;
            RequiresPassword = true;
            return Page();
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            StatusMessage = "Unable to set password. Please contact support.";
            return Page();
        }

        var oldHash = user.PasswordHash;
        var result = await userManager.AddPasswordAsync(user, Input.Password);

        if (!result.Succeeded)
        {
            EmailConfirmed = true;
            RequiresPassword = true;
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

        logger.LogInformation("User {UserId} set initial password after email confirmation", user.Id);

        await signInManager.SignInAsync(user, false);

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return Redirect(ReturnUrl);
        }

        return Redirect("~/");
    }

    public class InputModel
    {
        [Required]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}