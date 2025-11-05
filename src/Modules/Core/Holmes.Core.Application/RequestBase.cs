using System.Text.Json.Serialization;
using MediatR;

namespace Holmes.Core.Application;

public abstract record RequestBase : IRequest
{
    [JsonIgnore]
    public string? UserId { get; set; }
}

public abstract record RequestBase<T> : IRequest<T>
{
    [JsonIgnore]
    public string? UserId { get; set; }
}