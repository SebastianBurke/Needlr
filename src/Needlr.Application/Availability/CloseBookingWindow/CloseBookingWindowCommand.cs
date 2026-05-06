using Needlr.Application.Messaging;

namespace Needlr.Application.Availability.CloseBookingWindow;

/// <summary>
/// Removes a booking window. Implemented as a hard delete: any future request that would
/// have used this window now falls back to the artist's pattern + overrides.
/// </summary>
public sealed record CloseBookingWindowCommand(Guid WindowId) : ICommand;
