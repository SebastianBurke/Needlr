using Needlr.Application.Common.Geography;
using Needlr.Application.Portfolio;
using Needlr.Domain.Enums;

namespace Needlr.Application.Artists.GetArtistById;

/// <summary>
/// Public-facing artist profile + the bits the discovery / artist-profile UI needs:
/// computed verification status, primary studio info (so the map pin and the profile page
/// share a coordinate), and the artist's styles.
/// </summary>
public sealed record ArtistDetailDto(
    Guid Id,
    string DisplayName,
    string Bio,
    int YearsExperience,
    decimal? HourlyRateCad,
    decimal? ShopMinimumCad,
    bool AcceptingNewBookings,
    ArtistPaymentStatus PaymentStatus,
    CancellationPolicy CancellationPolicy,
    VerificationStatus VerificationStatus,
    PrimaryStudioSummaryDto? PrimaryStudio,
    IReadOnlyList<TattooStyleDto> Styles,
    BehavioralSignalsDto BehavioralSignals);

/// <summary>
/// Public behavioral signals (FEATURE_SPECS.md § Behavioral signals). Each metric is
/// nullable; null means the artist hasn't met the minimum-sample threshold and the FE
/// should suppress the badge rather than show a misleading number.
/// </summary>
public sealed record BehavioralSignalsDto(
    double? ResponseMedianHours,
    double? CompletionRatePercent,
    double? HealedPhotoRatePercent,
    double? RepeatClientRatePercent);

public sealed record PrimaryStudioSummaryDto(
    Guid Id,
    string Name,
    string Address,
    GeoPoint Location);
