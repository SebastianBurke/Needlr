using Needlr.Application.Messaging;

namespace Needlr.Application.Studios.GetStudioById;

public sealed record GetStudioByIdQuery(Guid StudioId) : IQuery<StudioDto>;
