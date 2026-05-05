using FluentAssertions;
using Needlr.Domain.Enums;
using Needlr.Domain.Identity;
using Xunit;

namespace Needlr.Domain.Tests.Identity;

public class ArtistLeadTimeTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid ArtistId = Guid.NewGuid();

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var lt = new ArtistLeadTime(Id, ArtistId, BookingType.TattooSession, minimumDays: 7);

        lt.Id.Should().Be(Id);
        lt.ArtistId.Should().Be(ArtistId);
        lt.BookingType.Should().Be(BookingType.TattooSession);
        lt.MinimumDays.Should().Be(7);
    }

    [Fact]
    public void Ctor_EmptyId_Throws()
    {
        var act = () => new ArtistLeadTime(Guid.Empty, ArtistId, BookingType.TattooSession, 7);
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Fact]
    public void Ctor_EmptyArtistId_Throws()
    {
        var act = () => new ArtistLeadTime(Id, Guid.Empty, BookingType.TattooSession, 7);
        act.Should().Throw<ArgumentException>().WithParameterName("artistId");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(ArtistLeadTime.MaxMinimumDays + 1)]
    public void Ctor_MinimumDaysOutOfRange_Throws(int days)
    {
        var act = () => new ArtistLeadTime(Id, ArtistId, BookingType.TattooSession, days);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("minimumDays");
    }
}
