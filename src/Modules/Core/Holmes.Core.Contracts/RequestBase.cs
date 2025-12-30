using System.Text.Json.Serialization;
using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Core.Contracts;

public interface IUserContextRequest
{
    string? UserId { get; set; }

    UlidId GetUserUlid();
}

public abstract record RequestBase : IRequest, IUserContextRequest
{
    [JsonIgnore]
    public string? UserId { get; set; }

    public UlidId GetUserUlid()
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            throw new InvalidOperationException("UserId was not populated.");
        }

        return UlidId.Parse(UserId);
    }
}

public abstract record RequestBase<T> : IRequest<T>, IUserContextRequest
{
    [JsonIgnore]
    public string? UserId { get; set; }

    public UlidId GetUserUlid()
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            throw new InvalidOperationException("UserId was not populated.");
        }

        return UlidId.Parse(UserId);
    }
}