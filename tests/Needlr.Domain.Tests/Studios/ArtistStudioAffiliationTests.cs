using FluentAssertions;
using Needlr.Domain.Enums;
using Needlr.Domain.Studios;
using Xunit;

namespace Needlr.Domain.Tests.Studios;

public class ArtistStudioAffiliationTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid ArtistId = Guid.NewGuid();
    private static readonly Guid StudioId = Guid.NewGuid();
    private static readonly DateOnly Start = new(2026, 1, 1);
    private static readonly DateOnly End = new(2026, 1, 31);

    [Fact]
    public void Ctor_PermanentAffiliation_NoEndDate_Succeeds()
    {
        var aff = new ArtistStudioAffiliation(Id, ArtistId, StudioId, AffiliationRole.Member, AffiliationType.Permanent, Start);

        aff.AffiliationType.Should().Be(AffiliationType.Permanent);
        aff.EndDate.Should().BeNull();
        aff.Status.Should().Be(AffiliationStatus.Pending);
    }

    [Fact]
    public void Ctor_GuestSpot_WithEndDate_Succeeds()
    {
        var aff = new ArtistStudioAffiliation(Id, ArtistId, StudioId, AffiliationRole.Member, AffiliationType.GuestSpot, Start, End);

        aff.EndDate.Should().Be(End);
    }

    [Fact]
    public void Ctor_GuestSpot_WithoutEndDate_Throws()
    {
        var act = () => new ArtistStudioAffiliation(Id, ArtistId, StudioId, AffiliationRole.Member, AffiliationType.GuestSpot, Start);
        act.Should().Throw<ArgumentException>().WithParameterName("endDate");
    }

    [Fact]
    public void Ctor_EndBeforeStart_Throws()
    {
        var act = () => new ArtistStudioAffiliation(Id, ArtistId, StudioId, AffiliationRole.Member, AffiliationType.GuestSpot, Start, Start.AddDays(-1));
        act.Should().Throw<ArgumentException>().WithParameterName("endDate");
    }

    [Fact]
    public void Ctor_EmptyArtistId_Throws()
    {
        var act = () => new ArtistStudioAffiliation(Id, Guid.Empty, StudioId, AffiliationRole.Member, AffiliationType.Permanent, Start);
        act.Should().Throw<ArgumentException>().WithParameterName("artistId");
    }

    [Fact]
    public void Ctor_EmptyStudioId_Throws()
    {
        var act = () => new ArtistStudioAffiliation(Id, ArtistId, Guid.Empty, AffiliationRole.Member, AffiliationType.Permanent, Start);
        act.Should().Throw<ArgumentException>().WithParameterName("studioId");
    }
}
