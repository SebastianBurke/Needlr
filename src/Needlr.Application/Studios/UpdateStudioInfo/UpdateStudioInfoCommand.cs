using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.Studios.UpdateStudioInfo;

public sealed record UpdateStudioInfoCommand(
    Guid StudioId,
    string Name,
    string Address,
    string? Description,
    JoinPolicy JoinPolicy) : ICommand;
