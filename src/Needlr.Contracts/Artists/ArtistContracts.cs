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
