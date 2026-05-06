using MediatR;
using Needlr.Application.Artists.GetArtistById;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Artists.GetMyArtist;

/// <summary>Returns the calling artist's full profile. Backs <c>GET /api/artists/me</c>.</summary>
public sealed record GetMyArtistQuery : IRequest<Result<ArtistDetailDto>>;
