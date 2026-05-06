using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Artists.UpdateArtistProfile;

/// <summary>
/// Updates the calling artist's editable profile fields. Bio, hourly rate, shop minimum,
/// years experience, and cancellation policy are all in scope. DisplayName is not editable
/// here — name changes are intentionally an admin/support flow to keep professional identity
/// stable across bookings. <c>AcceptingNewBookings</c> uses the dedicated toggle endpoint.
/// </summary>
public sealed record UpdateArtistProfileCommand(
    string Bio,
    int YearsExperience,
    decimal? HourlyRateCad,
    decimal? ShopMinimumCad,
    CancellationPolicy CancellationPolicy) : ICommand;
