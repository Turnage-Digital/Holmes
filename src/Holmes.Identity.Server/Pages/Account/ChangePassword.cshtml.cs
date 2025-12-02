using System.ComponentModel.DataAnnotations;
using Holmes.Identity.Server.Data;
using Holmes.Identity.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Holmes.Identity.Server.Pages.Account;

[AllowAnonymous]
public class ChangePasswordModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IPasswordHistoryService passwordHistoryService,
    TimeProvider timeProvider,
    IOptions<PasswordPolicyOptions> passwordOptions,
    ILogger<ChangePasswordModel> logger
) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }
    public bool IsExpired { get; set; }
    public bool ChangeComplete { get; set; }

    public class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(bool expired = false, string? returnUrl = null)
    {
        IsExpired = expired;
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (User.Identity?.IsAuthenticated == true)
        {
            Input.Email = User.Identity.Name ?? string.Empty;
        }
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }

        var passwordCheck = await userManager.CheckPasswordAsync(user, Input.CurrentPassword);
        if (!passwordCheck)
        {
            ModelState.AddModelError(string.Empty, "Current password is incorrect.");
            return Page();
        }

        var oldHash = user.PasswordHash;
        var result = await userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);

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

        logger.LogInformation("User {Email} changed their password", Input.Email);

        await signInManager.SignInAsync(user, isPersistent: false);

        ChangeComplete = true;
        return Page();
    }
}
