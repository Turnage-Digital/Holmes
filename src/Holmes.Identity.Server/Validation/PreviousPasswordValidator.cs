using Holmes.Identity.Server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Holmes.Identity.Server.Validation;

public class PreviousPasswordValidator(
    ApplicationDbContext dbContext,
    IOptions<PasswordPolicyOptions> options
) : IPasswordValidator<ApplicationUser>
{
    public async Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password
    )
    {
        if (string.IsNullOrEmpty(password))
        {
            return IdentityResult.Success;
        }

        var errors = new List<IdentityError>();

        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            var currentResult = manager.PasswordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                password);

            if (currentResult == PasswordVerificationResult.Success ||
                currentResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordReused",
                    Description = "You cannot reuse your current password."
                });
            }
        }

        var previousPasswords = await dbContext.UserPreviousPasswords
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Take(options.Value.PreviousPasswordCount)
            .Select(p => p.PasswordHash)
            .ToListAsync();

        foreach (var previousHash in previousPasswords)
        {
            var result = manager.PasswordHasher.VerifyHashedPassword(
                user,
                previousHash,
                password);

            if (result == PasswordVerificationResult.Success ||
                result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                errors.Add(new IdentityError
                {
                    Code = "PasswordPreviouslyUsed",
                    Description = $"You cannot reuse any of your last {options.Value.PreviousPasswordCount} passwords."
                });
                break;
            }
        }

        return errors.Count > 0
            ? IdentityResult.Failed(errors.ToArray())
            : IdentityResult.Success;
    }
}