using Needlr.Contracts.Affiliations;
using Needlr.Contracts.Artists;
using Needlr.Contracts.Auth;
using Needlr.Contracts.Bookings;
using Needlr.Contracts.Customers;
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

    /// <summary>Returns the seeded canonical tattoo styles for the discovery filter chips.</summary>
    Task<IReadOnlyList<TattooStyleResponse>> ListCanonicalStylesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Reads the calling artist's full profile (settings form preload).</summary>
    Task<ArtistDetailResponse> GetMyArtistAsync(CancellationToken cancellationToken = default);

    /// <summary>Updates the calling artist's editable profile fields.</summary>
    Task UpdateMyArtistAsync(
        UpdateArtistProfileRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reads the calling artist's accepting-new-bookings flag.</summary>
    Task<bool> GetMyAcceptingBookingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles the calling artist's accepting-new-bookings flag. Paused artists remain
    /// visible in discovery — only the booking action and the profile-side badge change.
    /// </summary>
    Task SetMyAcceptingBookingsAsync(
        bool accepting, CancellationToken cancellationToken = default);

    // ---- Customer self ----

    /// <summary>Reads the calling customer's profile.</summary>
    Task<MyCustomerProfileResponse> GetMyCustomerProfileAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Updates the calling customer's profile (display name).</summary>
    Task UpdateMyCustomerProfileAsync(
        UpdateMyCustomerProfileRequest request, CancellationToken cancellationToken = default);

    // ---- Studios + roster ----

    Task<StudioResponse> GetStudioAsync(Guid studioId, CancellationToken cancellationToken = default);
    Task<StudioRosterResponse> GetStudioRosterAsync(Guid studioId, CancellationToken cancellationToken = default);

    /// <summary>Flip a studio's accepts-walk-ins flag. Caller must be a studio admin.</summary>
    Task SetStudioWalkInsAsync(
        Guid studioId, bool acceptsWalkIns, CancellationToken cancellationToken = default);

    // ---- Artist + portfolio ----

    Task<ArtistDetailResponse> GetArtistAsync(Guid artistId, CancellationToken cancellationToken = default);

    Task<PagedPortfolioResponse> GetArtistPortfolioAsync(
        Guid artistId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    Task<PortfolioPieceResponse> GetPortfolioPieceAsync(
        Guid pieceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new portfolio piece for the calling artist with a fresh photo. Multipart
    /// upload — the file content is the body and the metadata fields are form values.
    /// </summary>
    Task<Guid> CreatePortfolioPieceAsync(
        CreatePortfolioPieceRequest meta,
        Stream fileContent,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes (soft-removes) a portfolio piece. Caller must own it.</summary>
    Task DeletePortfolioPieceAsync(Guid pieceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads an attachment (image/jpeg, image/png, image/webp; ≤10 MB) to an existing
    /// message. Both thread participants can attach; the API enforces ownership.
    /// </summary>
    Task<Guid> UploadMessageAttachmentAsync(
        Guid messageId,
        Stream content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Returns the booking-scoped thread for <paramref name="bookingId"/>, or <c>null</c> when
    /// the thread doesn't exist yet (pre-deposit-capture). Replaces the list-and-filter pattern
    /// in BookingDetail/ThreadView. 204 No Content on the wire is mapped to <c>null</c> here.
    /// </summary>
    Task<ThreadResponse?> GetThreadByBookingAsync(
        Guid bookingId, CancellationToken cancellationToken = default);

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

    // ---- Customer tooling (Phase 21) ----

    /// <summary>
    /// Customer uploads their healed photo for a Completed booking. Multipart upload —
    /// caller hands a content stream + content type + filename; the API resolves the
    /// piece linked to the booking and appends a <c>Healed</c> session photo.
    /// </summary>
    Task<Guid> UploadHealedPhotoAsync(
        Guid bookingId,
        Stream content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<Needlr.Contracts.Notifications.NotificationPreferencesResponse> GetMyNotificationPreferencesAsync(
        CancellationToken cancellationToken = default);

    Task UpdateMyNotificationPreferencesAsync(
        Needlr.Contracts.Notifications.UpdateNotificationPreferencesRequest request,
        CancellationToken cancellationToken = default);

    Task<Guid> RegisterPushSubscriptionAsync(
        Needlr.Contracts.Notifications.RegisterPushSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    Task UnregisterPushSubscriptionAsync(Guid subscriptionId, CancellationToken cancellationToken = default);

    // ---- Admin tooling (Phase 22) ----

    Task<IReadOnlyList<Needlr.Contracts.Verification.VerificationQueueItemResponse>> GetVerificationQueueAsync(
        CancellationToken cancellationToken = default);

    /// <summary>kind = "studio" or "artist".</summary>
    Task ReviewCredentialAsync(
        string kind, Guid credentialId,
        Needlr.Contracts.Verification.ReviewCredentialRequest request,
        CancellationToken cancellationToken = default);

    Task<Needlr.Contracts.TrustSafety.TrustSafetyDashboardResponse> GetTrustSafetyDashboardAsync(
        CancellationToken cancellationToken = default);

    Task SuspendUserAsync(
        Guid userId, Needlr.Contracts.TrustSafety.SuspendUserRequest request,
        CancellationToken cancellationToken = default);

    Task UnsuspendUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Guid> WarnUserAsync(
        Guid userId, Needlr.Contracts.TrustSafety.WarnUserRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Admin user search: paginated by email substring + optional role.</summary>
    Task<Needlr.Contracts.TrustSafety.AdminUserPageResponse> SearchUsersAsync(
        string? email, string? role, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default);

    // ---- Studio roster moderation ----

    /// <summary>The calling artist's own affiliations (across studios).</summary>
    Task<IReadOnlyList<AffiliationResponse>> ListMyAffiliationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Studio-admin roster view: all affiliations on the studio (Pending + Active + ...).</summary>
    Task<IReadOnlyList<StudioAffiliationResponse>> ListStudioAffiliationsAsync(
        Guid studioId, CancellationToken cancellationToken = default);

    /// <summary>Studio admin accepts/rejects a permanent join request from an artist.</summary>
    Task RespondToJoinRequestAsync(
        Guid affiliationId, bool accept, CancellationToken cancellationToken = default);

    /// <summary>Host studio admin accepts/rejects a guest-spot request.</summary>
    Task RespondToGuestSpotRequestAsync(
        Guid affiliationId, bool accept, CancellationToken cancellationToken = default);

    /// <summary>Send an invitation to an existing artist to join this studio.</summary>
    Task<Guid> InviteArtistToStudioAsync(
        Guid studioId, Guid artistId, CancellationToken cancellationToken = default);

    /// <summary>Change an existing affiliation's role (Founder/Admin/Member).</summary>
    Task ChangeAffiliationRoleAsync(
        Guid affiliationId, string newRole, CancellationToken cancellationToken = default);

    /// <summary>Mark an affiliation as the artist's primary studio.</summary>
    Task SetPrimaryAffiliationAsync(
        Guid affiliationId, CancellationToken cancellationToken = default);

    /// <summary>Remove an affiliation (deactivate).</summary>
    Task RemoveAffiliationAsync(
        Guid affiliationId, CancellationToken cancellationToken = default);
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
    bool AcceptsWalkInsOnly = false,
    IReadOnlyList<Guid>? StyleIds = null,
    DateOnly? AvailabilityFrom = null,
    DateOnly? AvailabilityTo = null,
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
