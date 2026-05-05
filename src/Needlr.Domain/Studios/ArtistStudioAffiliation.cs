using Needlr.Domain.Enums;

namespace Needlr.Domain.Studios;

/// <summary>
/// Time-boxed relationship between an artist and a studio. Supports permanent rosters and
/// guest spots. Per ADR-004, studio admin rights are encoded in <see cref="Role"/>; there is
/// no separate StudioOwner role.
/// </summary>
public sealed class ArtistStudioAffiliation
{
    public Guid Id { get; init; }
    public Guid ArtistId { get; init; }
    public Guid StudioId { get; init; }
    public AffiliationRole Role { get; set; }
    public AffiliationType AffiliationType { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; set; }
    public AffiliationStatus Status { get; set; }

    /// <summary>
    /// Whether this is the artist's primary affiliation. An artist may be affiliated with multiple
    /// studios; their primary determines their default discovery location.
    /// </summary>
    public bool IsPrimary { get; set; }

    private ArtistStudioAffiliation() { }

    public ArtistStudioAffiliation(
        Guid id,
        Guid artistId,
        Guid studioId,
        AffiliationRole role,
        AffiliationType affiliationType,
        DateOnly startDate,
        DateOnly? endDate = null,
        AffiliationStatus status = AffiliationStatus.Pending,
        bool isPrimary = false)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (artistId == Guid.Empty) throw new ArgumentException("ArtistId is required.", nameof(artistId));
        if (studioId == Guid.Empty) throw new ArgumentException("StudioId is required.", nameof(studioId));

        if (affiliationType is AffiliationType.GuestSpot && endDate is null)
            throw new ArgumentException("Guest spots require a non-null EndDate.", nameof(endDate));
        if (endDate is { } end && end < startDate)
            throw new ArgumentException("EndDate cannot be before StartDate.", nameof(endDate));

        Id = id;
        ArtistId = artistId;
        StudioId = studioId;
        Role = role;
        AffiliationType = affiliationType;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
        IsPrimary = isPrimary;
    }
}
