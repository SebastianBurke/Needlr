using Needlr.Contracts.Artists;
using Needlr.Contracts.Auth;
using Needlr.Contracts.Bookings;
using Needlr.Contracts.Discovery;
using Needlr.Contracts.Messaging;
using Needlr.Contracts.Portfolio;
using Needlr.Contracts.Studios;
using Needlr.Contracts.TrustSafety;

namespace Needlr.Contracts.Client;

/// <summary>
/// Typed HTTP client surface used by <c>Needlr.Web</c>. The auth slice plus the public
/// reads needed by Phase 17's discovery + studio + artist + portfolio UIs. Later phases
/// extend with bookings / messaging / notifications / artist tooling. Implementation
/// lives in <c>Needlr.Web</c> wrapping <see cref="HttpClient"/> with bearer-token injection
/// via a delegating handler.
/// </summary>
public interface INeedlrApi
{
    // ---- Auth ----

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RegisterCustomerAsync(
        RegisterCustomerRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RegisterArtistAsync(
        RegisterArtistRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default);

    // ---- Discovery (Phase 17) ----

    Task<DiscoveryPageResponse> SearchStudiosAsync(
        DiscoverySearchArgs args, CancellationToken cancellationToken = default);

    // ---- Studios + roster ----

    Task<StudioResponse> GetStudioAsync(Guid studioId, CancellationToken cancellationToken = default);
    Task<StudioRosterResponse> GetStudioRosterAsync(Guid studioId, CancellationToken cancellationToken = default);

    // ---- Artist + portfolio ----

    Task<ArtistDetailResponse> GetArtistAsync(Guid artistId, CancellationToken cancellationToken = default);

    Task<PagedPortfolioResponse> GetArtistPortfolioAsync(
        Guid artistId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<PortfolioPieceResponse> GetPortfolioPieceAsync(
        Guid pieceId, CancellationToken cancellationToken = default);

    // ---- Bookings (Phase 18) ----

    Task<Guid> RequestBookingAsync(
        RequestBookingRequest request, CancellationToken cancellationToken = default);

    Task<BookingPageResponse> ListMyBookingsAsCustomerAsync(
        string? status = null, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<BookingPageResponse> ListMyBookingsAsArtistAsync(
        string? status = null, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<BookingDetailResponse> GetBookingAsync(
        Guid bookingId, CancellationToken cancellationToken = default);

    Task AcceptBookingAsync(
        Guid bookingId, AcceptBookingRequest request, CancellationToken cancellationToken = default);

    Task DeclineBookingAsync(
        Guid bookingId, DeclineBookingRequest request, CancellationToken cancellationToken = default);

    Task RequestMoreInfoAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task RespondWithMoreInfoAsync(
        Guid bookingId, RespondWithMoreInfoRequest request, CancellationToken cancellationToken = default);

    Task MarkBookingInProgressAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task MarkBookingCompletedAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<CancelBookingResponse> CancelBookingByCustomerAsync(
        Guid bookingId, CancellationToken cancellationToken = default);

    Task<CancelBookingResponse> CancelBookingByArtistAsync(
        Guid bookingId, CancellationToken cancellationToken = default);

    Task<Guid> SubmitBookingFeedbackAsync(
        Guid bookingId, SubmitBookingFeedbackRequest request, CancellationToken cancellationToken = default);

    // ---- Messaging (Phase 19) ----

    Task<ThreadPageResponse> ListMyActiveThreadsAsync(
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<MessagePageResponse> ListThreadMessagesAsync(
        Guid threadId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    Task<Guid> SendMessageAsync(
        Guid threadId, SendMessageRequest request, CancellationToken cancellationToken = default);

    Task MarkMessageReadAsync(Guid messageId, CancellationToken cancellationToken = default);

    Task<Guid> ReportMessageAsync(
        Guid messageId, ReportMessageRequest request, CancellationToken cancellationToken = default);

    Task<int> GetUnreadMessageCountAsync(CancellationToken cancellationToken = default);

    // ---- Artist tooling (Phase 20) ----

    /// <summary>Creates the calling artist's Stripe Connect Express account. Idempotent.</summary>
    Task<ConnectAccountResponse> CreateConnectAccountAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a fresh hosted Stripe onboarding URL.</summary>
    Task<OnboardingLinkResponse> GenerateOnboardingLinkAsync(
        OnboardingLinkRequest request, CancellationToken cancellationToken = default);

    Task<Needlr.Contracts.Availability.AvailabilityPatternResponse> GetMyAvailabilityPatternAsync(
        CancellationToken cancellationToken = default);

    Task SetMyAvailabilityPatternAsync(
        Needlr.Contracts.Availability.SetAvailabilityPatternRequest request,
        CancellationToken cancellationToken = default);

    Task<Needlr.Contracts.Availability.AvailabilityOverridesResponse> ListMyAvailabilityOverridesAsync(
        DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default);

    Task AddMyAvailabilityOverrideAsync(
        Needlr.Contracts.Availability.AddAvailabilityOverrideRequest request,
        CancellationToken cancellationToken = default);

    Task RemoveMyAvailabilityOverrideAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<Needlr.Contracts.Availability.LeadTimesResponse> GetMyLeadTimesAsync(
        CancellationToken cancellationToken = default);

    Task SetMyLeadTimesAsync(
        Needlr.Contracts.Availability.SetLeadTimesRequest request,
        CancellationToken cancellationToken = default);

    Task<Needlr.Contracts.Availability.IcalFeedResponse> RotateIcalTokenAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Creates a studio with the calling artist as Founder + Admin.</summary>
    Task<Guid> CreateStudioAsync(CreateStudioRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Discovery query parameters. Mirrors the <c>GET /api/discovery/studios</c> query string;
/// the client converts this to the wire format.
/// </summary>
public sealed record DiscoverySearchArgs(
    double SouthLat,
    double WestLng,
    double NorthLat,
    double EastLng,
    double CenterLat,
    double CenterLng,
    bool VerifiedOnly = true,
    bool AcceptingNewBookingsOnly = true,
    IReadOnlyList<Guid>? StyleIds = null,
    DateOnly? AvailabilityFrom = null,
    DateOnly? AvailabilityTo = null,
    string Sort = "DistanceAscending",   // wire enum: DiscoverySort
    int Page = 1,
    int PageSize = 50);

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
