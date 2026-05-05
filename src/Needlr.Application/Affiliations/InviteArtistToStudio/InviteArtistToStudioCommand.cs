using Needlr.Application.Messaging;

namespace Needlr.Application.Affiliations.InviteArtistToStudio;

public sealed record InviteArtistToStudioCommand(Guid StudioId, Guid ArtistId) : ICommand<Guid>;
