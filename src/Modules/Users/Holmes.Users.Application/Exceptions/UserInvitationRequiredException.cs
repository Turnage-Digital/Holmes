namespace Holmes.Users.Application.Exceptions;

public sealed class UserInvitationRequiredException : Exception
{
    public UserInvitationRequiredException(string email, string issuer, string subject)
        : base($"User '{email}' must be invited before accessing Holmes.")
    {
        Email = email;
        Issuer = issuer;
        Subject = subject;
    }

    public string Email { get; }

    public string Issuer { get; }

    public string Subject { get; }
}