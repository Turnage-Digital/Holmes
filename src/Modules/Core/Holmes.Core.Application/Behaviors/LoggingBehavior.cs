using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Core.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        TResponse retval;
        if (request is RequestBase<TResponse> requestBase)
        {
            logger.LogInformation("Handling {request}",
                new { requestBase.GetType().Name, requestBase.UserId });

            retval = await next(cancellationToken);

            logger.LogInformation("Handled {request}",
                new { requestBase.GetType().Name, requestBase.UserId });
        }
        else
        {
            retval = await next(cancellationToken);
        }

        return retval;
    }
}