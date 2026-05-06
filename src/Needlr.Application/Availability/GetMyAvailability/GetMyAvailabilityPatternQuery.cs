using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;

namespace Needlr.Application.Availability.GetMyAvailability;

public sealed record GetMyAvailabilityPatternQuery : IQuery<IReadOnlyList<AvailabilityPatternDayDto>>;
