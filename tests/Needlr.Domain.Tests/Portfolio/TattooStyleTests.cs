using FluentAssertions;
using Needlr.Domain.Portfolio;
using Xunit;

namespace Needlr.Domain.Tests.Portfolio;

public class TattooStyleTests
{
    private static readonly Guid Id = Guid.NewGuid();

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var style = new TattooStyle(Id, "Blackwork", "blackwork", isCanonical: true);

        style.Id.Should().Be(Id);
        style.Name.Should().Be("Blackwork");
        style.Slug.Should().Be("blackwork");
        style.IsCanonical.Should().BeTrue();
    }

    [Fact]
    public void Ctor_NormalizesSlugToLowerInvariant()
    {
        var style = new TattooStyle(Id, "Blackwork", "BLACKwork", isCanonical: false);

        style.Slug.Should().Be("blackwork");
    }

    [Fact]
    public void Ctor_EmptyId_Throws()
    {
        var act = () => new TattooStyle(Guid.Empty, "n", "s", true);
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_BlankName_Throws(string name)
    {
        var act = () => new TattooStyle(Id, name, "s", true);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_BlankSlug_Throws(string slug)
    {
        var act = () => new TattooStyle(Id, "n", slug, true);
        act.Should().Throw<ArgumentException>().WithParameterName("slug");
    }
}
