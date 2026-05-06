using Needlr.Application.Messaging;

namespace Needlr.Application.Availability.Ical;

/// <summary>
/// Generates a fresh iCal token for the calling artist, replacing any prior token. Returns
/// the artist id + token so the API can construct the subscribe-able feed URL without
/// re-querying. Calling on an artist with no token yet effectively just creates one.
/// </summary>
public sealed record RotateIcalTokenCommand : ICommand<RotateIcalTokenResult>;

public sealed record RotateIcalTokenResult(Guid ArtistId, string Token);
