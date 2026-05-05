using Needlr.Domain.Enums;
using Needlr.Domain.Verification;

namespace Needlr.Application.Abstractions.Persistence;

public interface IArtistCredentialRepository
{
    Task<ArtistCredential?> GetByIdAsync(Guid credentialId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArtistCredential>> ListByStatusAsync(VerificationStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArtistCredential>> ListByArtistAsync(Guid artistId, CancellationToken cancellationToken = default);
    void Add(ArtistCredential credential);
}
