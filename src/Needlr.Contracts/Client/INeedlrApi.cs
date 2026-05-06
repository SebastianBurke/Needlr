using Needlr.Contracts.Auth;

namespace Needlr.Contracts.Client;

/// <summary>
/// Typed HTTP client surface used by <c>Needlr.Web</c>. Phase 16 ships the auth slice;
/// later phases extend with discovery / artists / bookings / messaging / notifications as
/// they ship to the FE. Implementation lives in <c>Needlr.Web</c> wrapping
/// <see cref="HttpClient"/> with bearer-token injection via a delegating handler.
/// </summary>
public interface INeedlrApi
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RegisterCustomerAsync(
        RegisterCustomerRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RegisterArtistAsync(
        RegisterArtistRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight failure carrier for the FE. The client throws this on any non-2xx
/// response; pages catch it to show user-facing error text.
/// </summary>
public sealed class NeedlrApiException : Exception
{
    public int StatusCode { get; }
    public string? ApiErrorCode { get; }

    public NeedlrApiException(int statusCode, string? apiErrorCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ApiErrorCode = apiErrorCode;
    }
}
