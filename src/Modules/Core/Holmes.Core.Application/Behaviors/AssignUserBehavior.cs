using Holmes.Core.Application.Abstractions;
using MediatR;

namespace Holmes.Core.Application.Behaviors;

public class AssignUserBehavior<TRequest, TResponse>(
    ICurrentUserInitializer currentUserInitializer
) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (request is not ISkipUserAssignment && request is IUserContextRequest userAwareRequest)
        {
            await EnsureUserIdAsync(userAwareRequest, cancellationToken);
        }

        return await next(cancellationToken);
    }

    private async Task EnsureUserIdAsync(IUserContextRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            return;
        }

        var userId = await currentUserInitializer.EnsureCurrentUserIdAsync(cancellationToken);
        request.UserId = userId.ToString();
    }
}