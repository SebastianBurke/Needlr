using Needlr.Domain.Identity;

namespace Needlr.Application.Abstractions.Persistence;

public interface IArtistRepository
{
    Task<Artist?> GetByIdAsync(Guid artistId, CancellationToken cancellationToken = default);

    /// <summary>Resolves the Artist row tied to an authenticated user (1:1 with ApplicationUser).</summary>
    Task<Artist?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid artistId, CancellationToken cancellationToken = default);
}
