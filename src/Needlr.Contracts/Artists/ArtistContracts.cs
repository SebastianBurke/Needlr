using Needlr.Contracts.Portfolio;
using Needlr.Contracts.Studios;

namespace Needlr.Contracts.Artists;

public sealed record ArtistDetailResponse(
    Guid Id,
    string DisplayName,
    string Bio,
    int YearsExperience,
    decimal? HourlyRateCad,
    decimal? ShopMinimumCad,
    bool AcceptingNewBookings,
    string PaymentStatus,
    string CancellationPolicy,
    string VerificationStatus,
    PrimaryStudioSummaryResponse? PrimaryStudio,
    IReadOnlyList<TattooStyleResponse> Styles,
    Needlr.Contracts.TrustSafety.BehavioralSignalsResponse BehavioralSignals);

public sealed record PrimaryStudioSummaryResponse(
    Guid Id,
    string Name,
    string Address,
    GeoPointDto Location);

// ---- Connect onboarding (Phase 20) ----

public sealed record ConnectAccountResponse(string ConnectAccountId);

public sealed record OnboardingLinkRequest(string? ReturnUrl, string? RefreshUrl);

public sealed record OnboardingLinkResponse(string Url);

// ---- Artist account settings ----

/// <summary>
/// Body for PUT /api/artists/me/accepting-bookings. Toggles whether new booking requests
/// can land. Paused artists remain visible in discovery + studio rosters.
/// </summary>
public sealed record SetAcceptingBookingsRequest(bool Accepting);

/// <summary>
/// Body for PATCH /api/artists/me. Editable artist-profile fields. DisplayName is not in
/// scope (intentionally — name changes go through admin to keep professional identity
/// stable across bookings). AcceptingNewBookings has its own dedicated toggle endpoint.
/// </summary>
public sealed record UpdateArtistProfileRequest(
    string Bio,
    int YearsExperience,
    decimal? HourlyRateCad,
    decimal? ShopMinimumCad,
    string CancellationPolicy);
