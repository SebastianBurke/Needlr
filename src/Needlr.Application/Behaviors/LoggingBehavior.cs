using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Needlr.Application.Behaviors;

/// <summary>
/// Logs request name + duration around every MediatR send. Outer-most behavior so its timing
/// covers validation and the database transaction.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Handling {Request}", requestName);

        try
        {
            var response = await next();
            sw.Stop();
            logger.LogInformation("Handled {Request} in {Elapsed}ms", requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "{Request} threw after {Elapsed}ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
