using System.Collections.ObjectModel;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain.Events;
using MediatR;

namespace Holmes.Users.Domain;

public sealed class User : AggregateRoot
{
    private readonly List<ExternalIdentity> _externalIdentities = [];
    private readonly List<RoleAssignment> _roles = [];

    private User()
    {
    }

    public UlidId Id { get; private set; }

    public string Email { get; private set; } = null!;

    public string? DisplayName { get; private set; }

    public UserStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<RoleAssignment> Roles => new ReadOnlyCollection<RoleAssignment>(_roles);

    public IReadOnlyCollection<ExternalIdentity> ExternalIdentities =>
        new ReadOnlyCollection<ExternalIdentity>(_externalIdentities);

    public static User Rehydrate(
        UlidId id,
        string email,
        string? displayName,
        UserStatus status,
        DateTimeOffset createdAt,
        IEnumerable<ExternalIdentity> identities,
        IEnumerable<RoleAssignment> roles
    )
    {
        var user = new User
        {
            Id = id,
            Email = email,
            DisplayName = displayName,
            Status = status,
            CreatedAt = createdAt
        };

        user._externalIdentities.AddRange(identities);
        user._roles.AddRange(roles);
        return user;
    }

    public static User Register(
        UlidId id,
        ExternalIdentity identity,
        string email,
        string? displayName,
        DateTimeOffset registeredAt
    )
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var user = new User();
        user.Apply(new UserRegistered(id, identity.Issuer, identity.Subject, email, displayName,
            identity.AuthenticationMethod, registeredAt));
        return user;
    }

    public static User Invite(
        UlidId id,
        string email,
        string? displayName,
        DateTimeOffset invitedAt
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var user = new User();
        user.Apply(new UserInvited(id, email, displayName, invitedAt));
        return user;
    }

    public void UpdateProfile(string email, string? displayName, DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        if (Email == email && DisplayName == displayName)
        {
            return;
        }

        Emit(new UserProfileUpdated(Id, email, displayName, timestamp));
        Email = email;
        DisplayName = displayName;
    }

    public void LinkExternalIdentity(ExternalIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (_externalIdentities.Any(e => e.Issuer == identity.Issuer && e.Subject == identity.Subject))
        {
            return;
        }

        _externalIdentities.Add(identity);
    }

    public void MarkIdentitySeen(string issuer, string subject, DateTimeOffset timestamp)
    {
        var existing = _externalIdentities.FirstOrDefault(i => i.Issuer == issuer && i.Subject == subject);
        if (existing is null)
        {
            return;
        }

        var updated = existing.Seen(timestamp);
        _externalIdentities.Remove(existing);
        _externalIdentities.Add(updated);
    }

    public void GrantRole(UserRole role, string? customerId, UlidId grantedBy, DateTimeOffset grantedAt)
    {
        if (_roles.Any(r => r.Role == role && r.CustomerId == customerId))
        {
            return;
        }

        var assignment = new RoleAssignment(role, customerId, grantedBy, grantedAt);
        _roles.Add(assignment);
        Emit(new UserRoleGranted(Id, role, customerId, grantedBy, grantedAt));
    }

    public void RevokeRole(UserRole role, string? customerId, UlidId revokedBy, DateTimeOffset revokedAt)
    {
        var assignment = _roles.FirstOrDefault(r => r.Role == role && r.CustomerId == customerId);
        if (assignment is null)
        {
            return;
        }

        if (role == UserRole.Admin && _roles.Count(r => r.Role == UserRole.Admin) == 1)
        {
            throw new InvalidOperationException("Cannot revoke the last global admin role.");
        }

        _roles.Remove(assignment);
        Emit(new UserRoleRevoked(Id, role, customerId, revokedBy, revokedAt));
    }

    public void Suspend(string reason, UlidId performedBy, DateTimeOffset suspendedAt)
    {
        if (Status == UserStatus.Suspended)
        {
            return;
        }

        Emit(new UserSuspended(Id, reason, performedBy, suspendedAt));
        Status = UserStatus.Suspended;
    }

    public void Reactivate(UlidId performedBy, DateTimeOffset reactivatedAt)
    {
        if (Status == UserStatus.Active)
        {
            return;
        }

        Emit(new UserReactivated(Id, performedBy, reactivatedAt));
        Status = UserStatus.Active;
    }

    public void ActivatePendingInvitation(DateTimeOffset activatedAt)
    {
        if (Status == UserStatus.Active)
        {
            return;
        }

        if (Status != UserStatus.PendingApproval)
        {
            throw new InvalidOperationException("Only pending invitations can be activated.");
        }

        Status = UserStatus.Active;
    }

    private void Apply(UserRegistered @event)
    {
        Id = @event.UserId;
        Email = @event.Email;
        DisplayName = @event.DisplayName;
        Status = UserStatus.Active;
        CreatedAt = @event.RegisteredAt;
        var identity = new ExternalIdentity(@event.Issuer, @event.Subject, @event.AuthenticationMethod,
            @event.RegisteredAt);
        _externalIdentities.Add(identity);
        Emit(@event);
    }

    private void Apply(UserInvited @event)
    {
        Id = @event.UserId;
        Email = @event.Email;
        DisplayName = @event.DisplayName;
        Status = UserStatus.PendingApproval;
        CreatedAt = @event.InvitedAt;
        Emit(@event);
    }

    private void Emit(INotification @event)
    {
        AddDomainEvent(@event);
    }
}