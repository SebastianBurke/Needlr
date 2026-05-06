using Needlr.Application.Messaging;

namespace Needlr.Application.Bookings.RequestMoreInfo;

/// <summary>
/// Artist asks the customer for more info on a booking request. v1 has no structured prompt
/// payload — the FE renders the standard checklist (better refs / clearer size /
/// alternative dates) and the customer responds via <c>RespondWithMoreInfoCommand</c>.
/// </summary>
public sealed record RequestMoreInfoCommand(Guid BookingId) : ICommand;
