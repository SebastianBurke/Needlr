using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Verification.GetVerificationQueue;

internal sealed class GetVerificationQueueQueryHandler(
    ICurrentUser currentUser,
    IStudioCredentialRepository studioCredentials,
    IArtistCredentialRepository artistCredentials)
    : IRequestHandler<GetVerificationQueueQuery, Result<IReadOnlyList<VerificationQueueItemDto>>>
{
    public async Task<Result<IReadOnlyList<VerificationQueueItemDto>>> Handle(
        GetVerificationQueueQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.Role != UserRole.Admin)
            return Result<IReadOnlyList<VerificationQueueItemDto>>.Failure(
                Error.Forbidden("Only admins can read the verification queue."));

        var studio = await studioCredentials.ListByStatusAsync(VerificationStatus.DocumentsSubmitted, cancellationToken);
        var artist = await artistCredentials.ListByStatusAsync(VerificationStatus.DocumentsSubmitted, cancellationToken);

        var items = new List<VerificationQueueItemDto>(studio.Count + artist.Count);
        items.AddRange(studio.Select(c => new VerificationQueueItemDto(
            c.Id, CredentialKind.Studio, c.StudioId, c.CredentialType.ToString(),
            c.DocumentUrl, c.IssuedDate, c.ExpiryDate)));
        items.AddRange(artist.Select(c => new VerificationQueueItemDto(
            c.Id, CredentialKind.Artist, c.ArtistId, c.CredentialType.ToString(),
            c.DocumentUrl, c.IssuedDate, c.ExpiryDate)));

        return Result<IReadOnlyList<VerificationQueueItemDto>>.Success(items);
    }
}
