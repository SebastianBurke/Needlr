using Microsoft.AspNetCore.Mvc;
using Needlr.Application.Common.Results;
using Needlr.Contracts.Common;

namespace Needlr.Api.Common;

/// <summary>
/// Maps <see cref="Result"/> / <see cref="Result{T}"/> into HTTP status codes by error code.
/// Keeps controller actions one-liners.
/// </summary>
internal static class ResultMapping
{
    public static IActionResult ToActionResult<T>(this Result<T> result, Func<T, object> onSuccess)
    {
        if (result.IsSuccess)
            return new OkObjectResult(onSuccess(result.Value!));
        return ToErrorResult(result);
    }

    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();
        return ToErrorResult(result);
    }

    private static IActionResult ToErrorResult(Result result)
    {
        var firstCode = result.FirstError?.Code ?? string.Empty;
        var status = firstCode switch
        {
            Error.UnauthorizedCode => StatusCodes.Status401Unauthorized,
            Error.ForbiddenCode => StatusCodes.Status403Forbidden,
            Error.NotFoundCode => StatusCodes.Status404NotFound,
            Error.ConflictCode => StatusCodes.Status409Conflict,
            Error.FailedPreconditionCode => StatusCodes.Status412PreconditionFailed,
            Error.ExternalServiceCode => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status400BadRequest
        };

        var apiErrors = result.Errors.Select(e => new ApiError(e.Code, e.Message)).ToArray();
        return new ObjectResult(new ApiErrorResponse(apiErrors)) { StatusCode = status };
    }
}
