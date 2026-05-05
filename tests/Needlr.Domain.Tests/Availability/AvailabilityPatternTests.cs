using FluentAssertions;
using Needlr.Domain.Availability;
using Needlr.Domain.Enums;
using Xunit;

namespace Needlr.Domain.Tests.Availability;

public class AvailabilityPatternTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid ArtistId = Guid.NewGuid();
    private static readonly DateOnly From = new(2026, 1, 1);

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var p = new AvailabilityPattern(Id, ArtistId, DayOfWeek.Tuesday, AvailabilityStatus.Available, From, maxSessionHours: 8m);

        p.Id.Should().Be(Id);
        p.ArtistId.Should().Be(ArtistId);
        p.DayOfWeek.Should().Be(DayOfWeek.Tuesday);
        p.Status.Should().Be(AvailabilityStatus.Available);
        p.EffectiveFrom.Should().Be(From);
        p.MaxSessionHours.Should().Be(8m);
    }

    [Fact]
    public void Ctor_EffectiveUntilBeforeFrom_Throws()
    {
        var act = () => new AvailabilityPattern(Id, ArtistId, DayOfWeek.Monday, AvailabilityStatus.Available, From, effectiveUntil: From.AddDays(-1));
        act.Should().Throw<ArgumentException>().WithParameterName("effectiveUntil");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(25)]
    public void Ctor_MaxSessionHoursOutOfRange_Throws(double hours)
    {
        var act = () => new AvailabilityPattern(Id, ArtistId, DayOfWeek.Monday, AvailabilityStatus.Available, From, maxSessionHours: (decimal)hours);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxSessionHours");
    }

    [Fact]
    public void Ctor_EmptyArtistId_Throws()
    {
        var act = () => new AvailabilityPattern(Id, Guid.Empty, DayOfWeek.Monday, AvailabilityStatus.Available, From);
        act.Should().Throw<ArgumentException>().WithParameterName("artistId");
    }
}
