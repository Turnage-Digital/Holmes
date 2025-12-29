using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Commands;

public sealed record ResumeOrderFromBlockCommand(
    UlidId OrderId,
    string Reason,
    DateTimeOffset ResumedAt
) : RequestBase<Result>;