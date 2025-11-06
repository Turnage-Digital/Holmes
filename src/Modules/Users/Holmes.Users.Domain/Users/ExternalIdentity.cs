using System;
using System.Collections.Generic;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Users.Domain.Users;

public sealed class ExternalIdentity : ValueObject
{
    public ExternalIdentity(string issuer, string subject, string? authenticationMethod, DateTimeOffset linkedAt)
        : this(issuer, subject, authenticationMethod, linkedAt, linkedAt)
    {
    }

    private ExternalIdentity(string issuer, string subject, string? authenticationMethod, DateTimeOffset linkedAt, DateTimeOffset lastSeenAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        Issuer = issuer;
        Subject = subject;
        AuthenticationMethod = authenticationMethod;
        LinkedAt = linkedAt;
        LastSeenAt = lastSeenAt;
    }

    public string Issuer { get; }

    public string Subject { get; }

    public string? AuthenticationMethod { get; }

    public DateTimeOffset LinkedAt { get; }

    public DateTimeOffset LastSeenAt { get; }

    public ExternalIdentity Seen(DateTimeOffset timestamp) =>
        new(Issuer, Subject, AuthenticationMethod, LinkedAt, timestamp);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Issuer;
        yield return Subject;
    }
}
