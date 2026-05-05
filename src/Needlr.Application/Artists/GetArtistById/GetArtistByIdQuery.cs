using Needlr.Application.Messaging;

namespace Needlr.Application.Artists.GetArtistById;

public sealed record GetArtistByIdQuery(Guid ArtistId) : IQuery<ArtistDetailDto>;
