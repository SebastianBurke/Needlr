using FluentAssertions;
using Needlr.Domain.Verification;
using Xunit;

namespace Needlr.Domain.Tests.Verification;

public class JurisdictionTests
{
    private static readonly Guid Id = Guid.NewGuid();

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var j = new Jurisdiction(
            Id, "Montréal, Quebec, Canada", "Canada", "Quebec", "Montréal",
            requiresStudioInspection: true,
            requiresArtistLicense: false,
            requiresArtistHygieneTraining: true,
            requiresBloodbornePathogenCert: true);

        j.Id.Should().Be(Id);
        j.Name.Should().Be("Montréal, Quebec, Canada");
        j.Country.Should().Be("Canada");
        j.Region.Should().Be("Quebec");
        j.City.Should().Be("Montréal");
        j.RequiresStudioInspection.Should().BeTrue();
        j.RequiresArtistLicense.Should().BeFalse();
        j.RequiresArtistHygieneTraining.Should().BeTrue();
        j.RequiresBloodbornePathogenCert.Should().BeTrue();
    }

    [Fact]
    public void Ctor_EmptyId_Throws()
    {
        var act = () => new Jurisdiction(Guid.Empty, "n", "c", "r", "ci", true, false, true, true);
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_BlankName_Throws(string name)
    {
        var act = () => new Jurisdiction(Id, name, "c", "r", "ci", true, false, true, true);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Ctor_BlankCountry_Throws()
    {
        var act = () => new Jurisdiction(Id, "n", "", "r", "ci", true, false, true, true);
        act.Should().Throw<ArgumentException>().WithParameterName("country");
    }

    [Fact]
    public void Ctor_BlankRegion_Throws()
    {
        var act = () => new Jurisdiction(Id, "n", "c", "", "ci", true, false, true, true);
        act.Should().Throw<ArgumentException>().WithParameterName("region");
    }

    [Fact]
    public void Ctor_BlankCity_Throws()
    {
        var act = () => new Jurisdiction(Id, "n", "c", "r", "", true, false, true, true);
        act.Should().Throw<ArgumentException>().WithParameterName("city");
    }
}
