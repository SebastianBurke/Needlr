using Needlr.Domain.Enums;
using Needlr.Domain.Verification;

namespace Needlr.Application.Abstractions.Persistence;

public interface IStudioCredentialRepository
{
    Task<StudioCredential?> GetByIdAsync(Guid credentialId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudioCredential>> ListByStatusAsync(VerificationStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudioCredential>> ListByStudioAsync(Guid studioId, CancellationToken cancellationToken = default);
    void Add(StudioCredential credential);
}
