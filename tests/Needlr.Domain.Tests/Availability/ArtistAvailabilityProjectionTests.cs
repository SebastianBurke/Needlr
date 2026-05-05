using FluentAssertions;
using Needlr.Domain.Availability;
using Xunit;

namespace Needlr.Domain.Tests.Availability;

public class ArtistAvailabilityProjectionTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid ArtistId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 6, 1);
    private static readonly DateTime RecomputedAt = DateTime.UtcNow;

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var p = new ArtistAvailabilityProjection(Id, ArtistId, Date, isBookable: true, remainingSessionHours: 6m, RecomputedAt);

        p.Id.Should().Be(Id);
        p.ArtistId.Should().Be(ArtistId);
        p.Date.Should().Be(Date);
        p.IsBookable.Should().BeTrue();
        p.RemainingSessionHours.Should().Be(6m);
        p.RecomputedAt.Should().Be(RecomputedAt);
    }

    [Fact]
    public void Ctor_NegativeRemainingHours_Throws()
    {
        var act = () => new ArtistAvailabilityProjection(Id, ArtistId, Date, true, -0.1m, RecomputedAt);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("remainingSessionHours");
    }

    [Fact]
    public void Ctor_NonUtcRecomputedAt_Throws()
    {
        var local = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var act = () => new ArtistAvailabilityProjection(Id, ArtistId, Date, true, 1m, local);
        act.Should().Throw<ArgumentException>().WithParameterName("recomputedAt");
    }

    [Fact]
    public void Ctor_EmptyArtistId_Throws()
    {
        var act = () => new ArtistAvailabilityProjection(Id, Guid.Empty, Date, true, 1m, RecomputedAt);
        act.Should().Throw<ArgumentException>().WithParameterName("artistId");
    }
}
