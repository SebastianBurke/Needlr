using Needlr.Application.Messaging;

namespace Needlr.Application.Affiliations.RequestGuestSpot;

public sealed record RequestGuestSpotCommand(
    Guid StudioId,
    DateOnly StartDate,
    DateOnly EndDate) : ICommand<Guid>;
