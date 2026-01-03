using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Orders.Application.Commands;

public sealed record ResumeOrderFromBlockCommand(
    UlidId OrderId,
    string Reason,
    DateTimeOffset ResumedAt
) : RequestBase<Result>;