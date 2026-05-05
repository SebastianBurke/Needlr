namespace Needlr.Contracts.Common;

/// <summary>
/// Structured error returned by the API for failed requests. <see cref="Code"/> is a stable
/// machine-readable identifier (matches <c>Needlr.Application.Common.Results.Error</c> codes);
/// <see cref="Message"/> is a short, human-readable explanation.
/// </summary>
public sealed record ApiError(string Code, string Message);

/// <summary>Wrapper used by error responses; carries one or more <see cref="ApiError"/>s.</summary>
public sealed record ApiErrorResponse(IReadOnlyList<ApiError> Errors);
