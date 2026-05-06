using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions;
using Needlr.Application.Common.Geography;
using Needlr.Application.Common.Pagination;
using Needlr.Application.Discovery;
using Needlr.Domain.Enums;

namespace Needlr.Infrastructure.Persistence.Discovery;

internal sealed class ArtistDiscoveryService(NeedlrDbContext db) : IArtistDiscoveryService
{
    private readonly NeedlrDbContext _db = db;

    public async Task<PagedResult<DiscoveryStudioDto>> SearchAsync(
        DiscoverySearchCriteria criteria, CancellationToken cancellationToken = default)
    {
        var page = criteria.Page.Clamp();
        var bounds = criteria.Bounds;
        var centerPoint = criteria.Center.ToPoint();

        // Bounding-box filter on the Point's X/Y. We use literal axis comparisons rather than
        // ST_Within because the planner picks them up cleanly with PostGIS' GiST index on
        // location, and they sidestep the antimeridian-wrap edge case (which Montréal-only v1
        // doesn't hit).
        IQueryable<Domain.Studios.Studio> query = _db.Studios
            .Where(s =>
                s.Location.Y >= bounds.SouthLat && s.Location.Y <= bounds.NorthLat
                && s.Location.X >= bounds.WestLng && s.Location.X <= bounds.EastLng);

        // Studio-level verification gate. Per FEATURE_SPECS.md § Discoverability rules:
        //   Verified-only on  → studio has at least one Verified HealthInspection credential
        //   Verified-only off → studio has at least one Verified or DocumentsSubmitted credential
        //   Unverified/Rejected studios are never shown in discovery.
        if (criteria.VerifiedOnly)
        {
            query = query.Where(s => _db.StudioCredentials.Any(c =>
                c.StudioId == s.Id
                && c.CredentialType == StudioCredentialType.HealthInspection
                && c.VerificationStatus == VerificationStatus.Verified));
        }
        else
        {
            query = query.Where(s => _db.StudioCredentials.Any(c =>
                c.StudioId == s.Id
                && c.CredentialType == StudioCredentialType.HealthInspection
                && (c.VerificationStatus == VerificationStatus.Verified
                    || c.VerificationStatus == VerificationStatus.DocumentsSubmitted)));
        }

        // Style + accepting-bookings filter folds to "studio has at least one Active artist
        // matching". When neither is constrained we still require an Active affiliation, so
        // empty-roster studios don't appear on the map.
        var styleIds = criteria.StyleIds ?? Array.Empty<Guid>();
        var styleListNotEmpty = styleIds.Count > 0;
        var requireBookings = criteria.AcceptingNewBookingsOnly;

        query = query.Where(s => _db.ArtistStudioAffiliations.Any(a =>
            a.StudioId == s.Id
            && a.Status == AffiliationStatus.Active
            && _db.Artists.Any(art =>
                art.Id == a.ArtistId
                && (!requireBookings || art.AcceptingNewBookings)
                // Suspended artists are invisible per FEATURE_SPECS § Admin actions.
                && !_db.Users.Any(u => u.Id == art.UserId && u.SuspendedAt != null)
                && (!styleListNotEmpty || art.Styles.Any(st => styleIds.Contains(st.Id))))));

        // Availability filter: studio has at least one Active artist with at least one bookable
        // day in [from, to]. Joins ArtistAvailabilityProjection — that table is empty until
        // Phase 9 wires the projector, so callers passing this filter before then will get
        // empty results. Intentional scaffolding per BUILD_PLAN.md.
        if (criteria.AvailabilityFrom is { } from && criteria.AvailabilityTo is { } to)
        {
            query = query.Where(s => _db.ArtistStudioAffiliations.Any(a =>
                a.StudioId == s.Id
                && a.Status == AffiliationStatus.Active
                && _db.ArtistAvailabilityProjections.Any(p =>
                    p.ArtistId == a.ArtistId
                    && p.IsBookable
                    && p.Date >= from
                    && p.Date <= to)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var sorted = ApplySort(query, criteria.Sort, centerPoint, criteria.AvailabilityFrom, criteria.AvailabilityTo);

        var rows = await sorted
            .Skip(page.Skip).Take(page.PageSize)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Address,
                s.StudioType,
                LocationX = s.Location.X,
                LocationY = s.Location.Y,
                Distance = s.Location.Distance(centerPoint),
                IsVerified = _db.StudioCredentials.Any(c =>
                    c.StudioId == s.Id
                    && c.CredentialType == StudioCredentialType.HealthInspection
                    && c.VerificationStatus == VerificationStatus.Verified),
                HasSubmittedDocuments = _db.StudioCredentials.Any(c =>
                    c.StudioId == s.Id
                    && c.CredentialType == StudioCredentialType.HealthInspection
                    && c.VerificationStatus == VerificationStatus.DocumentsSubmitted),
                ActiveArtistCount = _db.ArtistStudioAffiliations.Count(a =>
                    a.StudioId == s.Id && a.Status == AffiliationStatus.Active)
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new DiscoveryStudioDto(
            r.Id,
            r.Name,
            r.Address,
            r.StudioType,
            new GeoPoint(Latitude: r.LocationY, Longitude: r.LocationX),
            r.Distance,
            r.IsVerified,
            r.HasSubmittedDocuments,
            r.ActiveArtistCount)).ToList();

        return new PagedResult<DiscoveryStudioDto>(items, page.Page, page.PageSize, totalCount);
    }

    private IQueryable<Domain.Studios.Studio> ApplySort(
        IQueryable<Domain.Studios.Studio> query,
        DiscoverySort sort,
        NetTopologySuite.Geometries.Point center,
        DateOnly? from,
        DateOnly? to) => sort switch
    {
        DiscoverySort.DistanceAscending => query.OrderBy(s => s.Location.Distance(center)),

        DiscoverySort.VerifiedFirst => query
            // Studio is "Verified" iff it has a Verified HealthInspection credential.
            .OrderByDescending(s => _db.StudioCredentials.Any(c =>
                c.StudioId == s.Id
                && c.CredentialType == StudioCredentialType.HealthInspection
                && c.VerificationStatus == VerificationStatus.Verified))
            .ThenBy(s => s.Location.Distance(center)),

        DiscoverySort.AvailabilitySoonness => query
            // Min bookable date across the studio's Active artists in the requested window.
            // Studios with no projection rows for the window sort to the end (effectively
            // MaxValue), then fall back to distance.
            .OrderBy(s => _db.ArtistStudioAffiliations
                .Where(a => a.StudioId == s.Id && a.Status == AffiliationStatus.Active)
                .SelectMany(a => _db.ArtistAvailabilityProjections
                    .Where(p => p.ArtistId == a.ArtistId
                        && p.IsBookable
                        && (from == null || p.Date >= from)
                        && (to == null || p.Date <= to))
                    .Select(p => (DateOnly?)p.Date))
                .Min() ?? DateOnly.MaxValue)
            .ThenBy(s => s.Location.Distance(center)),

        _ => query.OrderBy(s => s.Location.Distance(center))
    };
}
