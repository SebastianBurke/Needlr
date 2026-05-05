using FluentAssertions;
using Needlr.Domain.Availability;
using Needlr.Domain.Enums;
using Xunit;

namespace Needlr.Domain.Tests.Availability;

public class AvailabilityOverrideTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid ArtistId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 8, 1);

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var o = new AvailabilityOverride(Id, ArtistId, Date, AvailabilityStatus.Closed, reason: "Vacation");

        o.Id.Should().Be(Id);
        o.ArtistId.Should().Be(ArtistId);
        o.Date.Should().Be(Date);
        o.Status.Should().Be(AvailabilityStatus.Closed);
        o.Reason.Should().Be("Vacation");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    public void Ctor_MaxSessionHoursOutOfRange_Throws(double hours)
    {
        var act = () => new AvailabilityOverride(Id, ArtistId, Date, AvailabilityStatus.Limited, maxSessionHours: (decimal)hours);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxSessionHours");
    }

    [Fact]
    public void Ctor_ReasonTooLong_Throws()
    {
        var reason = new string('a', AvailabilityOverride.ReasonMaxLength + 1);
        var act = () => new AvailabilityOverride(Id, ArtistId, Date, AvailabilityStatus.Closed, reason: reason);
        act.Should().Throw<ArgumentException>().WithParameterName("reason");
    }

    [Fact]
    public void Ctor_EmptyArtistId_Throws()
    {
        var act = () => new AvailabilityOverride(Id, Guid.Empty, Date, AvailabilityStatus.Closed);
        act.Should().Throw<ArgumentException>().WithParameterName("artistId");
    }
}
