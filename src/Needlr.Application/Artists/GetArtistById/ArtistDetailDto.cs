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
    IReadOnlyList<TattooStyleDto> Styles);

public sealed record PrimaryStudioSummaryDto(
    Guid Id,
    string Name,
    string Address,
    GeoPoint Location);
