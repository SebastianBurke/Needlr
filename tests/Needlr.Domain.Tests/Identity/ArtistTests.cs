using FluentAssertions;
using Needlr.Domain.Enums;
using Needlr.Domain.Identity;
using Xunit;

namespace Needlr.Domain.Tests.Identity;

public class ArtistTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var artist = new Artist(Id, UserId, "Inkling", "I tattoo birds.", yearsExperience: 5, hourlyRateCad: 150m, shopMinimumCad: 100m);

        artist.Id.Should().Be(Id);
        artist.UserId.Should().Be(UserId);
        artist.DisplayName.Should().Be("Inkling");
        artist.Bio.Should().Be("I tattoo birds.");
        artist.YearsExperience.Should().Be(5);
        artist.HourlyRateCad.Should().Be(150m);
        artist.ShopMinimumCad.Should().Be(100m);
        artist.AcceptingNewBookings.Should().BeTrue();
        artist.PaymentStatus.Should().Be(ArtistPaymentStatus.NotOnboarded);
        artist.CancellationPolicy.Should().Be(CancellationPolicy.Standard);
    }

    [Fact]
    public void Ctor_NullBio_BecomesEmptyString()
    {
        var artist = new Artist(Id, UserId, "Inkling", bio: null!, yearsExperience: 1);

        artist.Bio.Should().BeEmpty();
    }

    [Fact]
    public void Ctor_EmptyId_Throws()
    {
        var act = () => new Artist(Guid.Empty, UserId, "Inkling", string.Empty, 1);
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Fact]
    public void Ctor_EmptyUserId_Throws()
    {
        var act = () => new Artist(Id, Guid.Empty, "Inkling", string.Empty, 1);
        act.Should().Throw<ArgumentException>().WithParameterName("userId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_BlankDisplayName_Throws(string name)
    {
        var act = () => new Artist(Id, UserId, name, string.Empty, 1);
        act.Should().Throw<ArgumentException>().WithParameterName("displayName");
    }

    [Fact]
    public void Ctor_BioTooLong_Throws()
    {
        var bio = new string('a', Artist.BioMaxLength + 1);
        var act = () => new Artist(Id, UserId, "Inkling", bio, 1);
        act.Should().Throw<ArgumentException>().WithParameterName("bio");
    }

    [Fact]
    public void Ctor_NegativeYearsExperience_Throws()
    {
        var act = () => new Artist(Id, UserId, "Inkling", string.Empty, yearsExperience: -1);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("yearsExperience");
    }

    [Fact]
    public void Ctor_NegativeHourlyRate_Throws()
    {
        var act = () => new Artist(Id, UserId, "Inkling", string.Empty, 1, hourlyRateCad: -1m);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("hourlyRateCad");
    }

    [Fact]
    public void Ctor_NegativeShopMinimum_Throws()
    {
        var act = () => new Artist(Id, UserId, "Inkling", string.Empty, 1, shopMinimumCad: -1m);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("shopMinimumCad");
    }
}
