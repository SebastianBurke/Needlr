using FluentAssertions;
using NetTopologySuite.Geometries;
using Needlr.Domain.Enums;
using Needlr.Domain.Studios;
using Xunit;

namespace Needlr.Domain.Tests.Studios;

public class StudioTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid CreatedByArtistId = Guid.NewGuid();
    private static readonly Point Location = new(-73.5674, 45.5019) { SRID = 4326 };

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var studio = new Studio(Id, "Black Needle", StudioType.Shop, Location, "1 Rue Saint-Denis, Montréal", CreatedByArtistId);

        studio.Id.Should().Be(Id);
        studio.Name.Should().Be("Black Needle");
        studio.StudioType.Should().Be(StudioType.Shop);
        studio.Location.Should().BeSameAs(Location);
        studio.Address.Should().Be("1 Rue Saint-Denis, Montréal");
        studio.CreatedByArtistId.Should().Be(CreatedByArtistId);
    }

    [Fact]
    public void Ctor_AcceptsWalkIns_DefaultsToFalse()
    {
        // Walk-ins is opt-in: a newly created studio is appointment-only until the artist
        // explicitly toggles it on. Discovery's walk-ins filter would otherwise sweep up
        // every fresh studio.
        var studio = new Studio(Id, "X", StudioType.Shop, Location, "a", CreatedByArtistId);

        studio.AcceptsWalkIns.Should().BeFalse();
    }

    [Theory]
    [InlineData(StudioType.Shop, JoinPolicy.InviteOnly)]
    [InlineData(StudioType.Solo, JoinPolicy.Closed)]
    [InlineData(StudioType.Private, JoinPolicy.InviteOnly)]
    public void Ctor_DefaultsJoinPolicyByStudioType(StudioType type, JoinPolicy expected)
    {
        var studio = new Studio(Id, "X", type, Location, "addr", CreatedByArtistId);

        studio.JoinPolicy.Should().Be(expected);
    }

    [Fact]
    public void Ctor_ExplicitJoinPolicy_OverridesDefault()
    {
        var studio = new Studio(Id, "X", StudioType.Solo, Location, "addr", CreatedByArtistId, joinPolicy: JoinPolicy.Open);

        studio.JoinPolicy.Should().Be(JoinPolicy.Open);
    }

    [Fact]
    public void Ctor_EmptyId_Throws()
    {
        var act = () => new Studio(Guid.Empty, "X", StudioType.Shop, Location, "a", CreatedByArtistId);
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_BlankName_Throws(string name)
    {
        var act = () => new Studio(Id, name, StudioType.Shop, Location, "a", CreatedByArtistId);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Ctor_NameTooLong_Throws()
    {
        var name = new string('a', Studio.NameMaxLength + 1);
        var act = () => new Studio(Id, name, StudioType.Shop, Location, "a", CreatedByArtistId);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Ctor_NullLocation_Throws()
    {
        var act = () => new Studio(Id, "X", StudioType.Shop, null!, "a", CreatedByArtistId);
        act.Should().Throw<ArgumentNullException>().WithParameterName("location");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_BlankAddress_Throws(string addr)
    {
        var act = () => new Studio(Id, "X", StudioType.Shop, Location, addr, CreatedByArtistId);
        act.Should().Throw<ArgumentException>().WithParameterName("address");
    }

    [Fact]
    public void Ctor_EmptyCreatedByArtistId_Throws()
    {
        var act = () => new Studio(Id, "X", StudioType.Shop, Location, "a", Guid.Empty);
        act.Should().Throw<ArgumentException>().WithParameterName("createdByArtistId");
    }

    [Fact]
    public void Ctor_DescriptionTooLong_Throws()
    {
        var desc = new string('a', Studio.DescriptionMaxLength + 1);
        var act = () => new Studio(Id, "X", StudioType.Shop, Location, "a", CreatedByArtistId, description: desc);
        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }
}
