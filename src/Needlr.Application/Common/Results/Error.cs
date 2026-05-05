namespace Needlr.Application.Common.Results;

/// <summary>
/// Stable, code-keyed failure descriptor returned inside <see cref="Result"/> /
/// <see cref="Result{T}"/>. The <see cref="Code"/> is intended to be machine-readable so
/// API responses can map error categories to HTTP status codes uniformly.
/// </summary>
public sealed record Error(string Code, string Message)
{
    public const string NotFoundCode = "not_found";
    public const string ValidationCode = "validation";
    public const string UnauthorizedCode = "unauthorized";
    public const string ForbiddenCode = "forbidden";
    public const string ConflictCode = "conflict";
    public const string FailedPreconditionCode = "failed_precondition";
    public const string ExternalServiceCode = "external_service";

    public static Error NotFound(string entity) => new(NotFoundCode, $"{entity} not found.");
    public static Error Validation(string message) => new(ValidationCode, message);
    public static Error Unauthorized(string message = "Unauthorized.") => new(UnauthorizedCode, message);
    public static Error Forbidden(string message = "Forbidden.") => new(ForbiddenCode, message);
    public static Error Conflict(string message) => new(ConflictCode, message);
    public static Error FailedPrecondition(string message) => new(FailedPreconditionCode, message);
    public static Error ExternalService(string message) => new(ExternalServiceCode, message);
}
