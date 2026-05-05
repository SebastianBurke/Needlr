using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Geography;
using Needlr.Application.Common.Results;
using Needlr.Application.Portfolio;
using Needlr.Domain.Enums;

namespace Needlr.Application.Artists.GetArtistById;

internal sealed class GetArtistByIdQueryHandler(
    IArtistRepository artists,
    IArtistStudioAffiliationRepository affiliations,
    IStudioRepository studios,
    IVerificationStatusService verification)
    : IRequestHandler<GetArtistByIdQuery, Result<ArtistDetailDto>>
{
    public async Task<Result<ArtistDetailDto>> Handle(GetArtistByIdQuery request, CancellationToken cancellationToken)
    {
        var artist = await artists.GetByIdWithStylesAsync(request.ArtistId, cancellationToken);
        if (artist is null)
            return Result<ArtistDetailDto>.Failure(Error.NotFound("Artist"));

        var allAffiliations = await affiliations.ListByArtistAsync(artist.Id, cancellationToken);
        var primary = allAffiliations.FirstOrDefault(a =>
            a.IsPrimary && a.Status == AffiliationStatus.Active);

        PrimaryStudioSummaryDto? primaryStudio = null;
        if (primary is not null)
        {
            var studio = await studios.GetByIdAsync(primary.StudioId, cancellationToken);
            if (studio is not null)
            {
                primaryStudio = new PrimaryStudioSummaryDto(
                    studio.Id, studio.Name, studio.Address, studio.Location.ToGeoPoint());
            }
        }

        var status = await verification.ComputeArtistStatusAsync(artist.Id, cancellationToken);

        var styles = artist.Styles
            .Select(s => new TattooStyleDto(s.Id, s.Name, s.Slug, s.IsCanonical))
            .ToList();

        var dto = new ArtistDetailDto(
            artist.Id,
            artist.DisplayName,
            artist.Bio,
            artist.YearsExperience,
            artist.HourlyRateCad,
            artist.ShopMinimumCad,
            artist.AcceptingNewBookings,
            artist.PaymentStatus,
            artist.CancellationPolicy,
            status,
            primaryStudio,
            styles);

        return Result<ArtistDetailDto>.Success(dto);
    }
}
