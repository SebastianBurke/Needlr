using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;

namespace Needlr.Infrastructure.Identity;

internal sealed class StudioAuthorization(
    ICurrentUser currentUser,
    IArtistRepository artistRepository,
    IArtistStudioAffiliationRepository affiliations) : IStudioAuthorization
{
    public async Task<bool> IsCurrentUserStudioAdminAsync(Guid studioId, CancellationToken cancellationToken = default)
    {
        var artistId = await GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null) return false;
        return await affiliations.IsAdminAsync(artistId.Value, studioId, cancellationToken);
    }

    public async Task<Guid?> GetCurrentArtistIdAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (userId is null) return null;
        var artist = await artistRepository.GetByUserIdAsync(userId.Value, cancellationToken);
        return artist?.Id;
    }
}
