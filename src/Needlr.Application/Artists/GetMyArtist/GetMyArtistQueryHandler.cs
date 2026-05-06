using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Artists.GetArtistById;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Artists.GetMyArtist;

/// <summary>
/// Resolves the calling artist's id from the auth context, then delegates to the existing
/// <see cref="GetArtistByIdQuery"/> pipeline so all derived data (verification status,
/// behavioral signals, primary studio, styles) flows through one code path.
/// </summary>
internal sealed class GetMyArtistQueryHandler(
    IStudioAuthorization studioAuthorization,
    IMediator mediator)
    : IRequestHandler<GetMyArtistQuery, Result<ArtistDetailDto>>
{
    public async Task<Result<ArtistDetailDto>> Handle(GetMyArtistQuery request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<ArtistDetailDto>.Failure(Error.Forbidden("Only artists can read their own profile."));

        return await mediator.Send(new GetArtistByIdQuery(artistId.Value), cancellationToken);
    }
}
