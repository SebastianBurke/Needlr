using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Needlr.Application.Common.Results;
using Needlr.Contracts.Common;

namespace Needlr.Api.Common;

/// <summary>
/// Translates exceptions thrown from MediatR pipeline behaviors into uniform JSON error
/// responses. Currently handles <see cref="ValidationException"/> (from
/// <c>ValidationBehavior</c>) → 400 with per-property errors. Other unhandled exceptions
/// fall through to the framework's default 500.
/// </summary>
internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validation)
            return false;

        logger.LogInformation(
            "Validation failure on {Path}: {Errors}",
            httpContext.Request.Path,
            string.Join("; ", validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        var errors = validation.Errors
            .Select(e => new ApiError(Error.ValidationCode, e.ErrorMessage))
            .ToArray();
        await httpContext.Response.WriteAsJsonAsync(new ApiErrorResponse(errors), cancellationToken);
        return true;
    }
}
