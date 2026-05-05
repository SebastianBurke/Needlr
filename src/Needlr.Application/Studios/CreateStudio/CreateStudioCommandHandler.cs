using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Geography;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;
using Needlr.Domain.Studios;

namespace Needlr.Application.Studios.CreateStudio;

internal sealed class CreateStudioCommandHandler(
    IStudioAuthorization studioAuthorization,
    IStudioRepository studios,
    IArtistStudioAffiliationRepository affiliations,
    IClock clock) : IRequestHandler<CreateStudioCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateStudioCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<Guid>.Failure(Error.Forbidden("Only artists can create studios."));

        var studioId = Guid.NewGuid();
        var studio = new Studio(
            id: studioId,
            name: request.Name,
            studioType: request.StudioType,
            location: request.Location.ToPoint(),
            address: request.Address,
            createdByArtistId: artistId.Value,
            joinPolicy: request.JoinPolicy,
            description: request.Description);
        studios.Add(studio);

        // Founding affiliation. IsPrimary is true if the artist has no existing Active primary.
        var existing = await affiliations.ListByArtistAsync(artistId.Value, cancellationToken);
        var hasPrimary = existing.Any(a =>
            a.IsPrimary && a.Status == AffiliationStatus.Active);

        var founding = new ArtistStudioAffiliation(
            id: Guid.NewGuid(),
            artistId: artistId.Value,
            studioId: studio.Id,
            role: AffiliationRole.Founder,
            affiliationType: AffiliationType.Permanent,
            startDate: DateOnly.FromDateTime(clock.UtcNow),
            status: AffiliationStatus.Active,
            isPrimary: !hasPrimary);
        affiliations.Add(founding);

        return Result<Guid>.Success(studio.Id);
    }
}
