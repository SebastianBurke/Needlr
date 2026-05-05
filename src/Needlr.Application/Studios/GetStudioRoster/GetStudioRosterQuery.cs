using Needlr.Application.Messaging;

namespace Needlr.Application.Studios.GetStudioRoster;

public sealed record GetStudioRosterQuery(Guid StudioId) : IQuery<StudioRosterDto>;
