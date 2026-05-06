using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;

namespace Needlr.Application.Availability.Ical;

/// <summary>
/// Anonymous query consumed by the public iCal endpoint. The artistId + token combination
/// is the auth: a wrong or rotated token returns NotFound. Returns the rendered VCALENDAR
/// body as a string ready for <c>Content-Type: text/calendar</c>.
/// </summary>
public sealed record GetIcalFeedQuery(Guid ArtistId, string Token) : IQuery<string>;
