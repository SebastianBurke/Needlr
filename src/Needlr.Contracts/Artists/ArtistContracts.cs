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
    IReadOnlyList<TattooStyleResponse> Styles);

public sealed record PrimaryStudioSummaryResponse(
    Guid Id,
    string Name,
    string Address,
    GeoPointDto Location);
