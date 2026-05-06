using Needlr.Application.Messaging;

namespace Needlr.Application.Availability.SetLeadTimes;

/// <summary>
/// Replaces the calling artist's lead-time rules. Defaults per FEATURE_SPECS.md § Lead time
/// (Consultation 3, TattooSession 7, Touchup 7) are applied at registration; this command
/// overrides them. The full set must be supplied — partial updates aren't supported because
/// the booking-request flow consults all three booking types and gaps are ambiguous.
/// </summary>
public sealed record SetLeadTimesCommand(IReadOnlyList<LeadTimeDto> LeadTimes) : ICommand;
