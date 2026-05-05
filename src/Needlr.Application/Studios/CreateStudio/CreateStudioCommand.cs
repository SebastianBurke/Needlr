using Needlr.Application.Common.Geography;
using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Studios.CreateStudio;

/// <summary>
/// Creates a new studio with the calling artist as Founder + Admin (Active affiliation,
/// becomes the artist's primary if they have no existing primary). JoinPolicy defaults per
/// FEATURE_SPECS.md § Studio types via <c>Studio.DefaultJoinPolicyFor</c>.
/// </summary>
public sealed record CreateStudioCommand(
    string Name,
    StudioType StudioType,
    GeoPoint Location,
    string Address,
    JoinPolicy? JoinPolicy = null,
    string? Description = null) : ICommand<Guid>;
