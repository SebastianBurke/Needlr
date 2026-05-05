using FluentAssertions;
using Needlr.Domain.Identity;
using Xunit;

namespace Needlr.Domain.Tests.Identity;

public class CustomerProfileTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var profile = new CustomerProfile(Id, UserId, "Alice", preferredSearchRadiusKm: 25);

        profile.Id.Should().Be(Id);
        profile.UserId.Should().Be(UserId);
        profile.DisplayName.Should().Be("Alice");
        profile.PreferredSearchRadiusKm.Should().Be(25);
        profile.Location.Should().BeNull();
        profile.PreferredStyles.Should().BeEmpty();
    }

    [Fact]
    public void Ctor_DefaultRadius_IsApplied()
    {
        var profile = new CustomerProfile(Id, UserId, "Alice");

        profile.PreferredSearchRadiusKm.Should().Be(CustomerProfile.DefaultSearchRadiusKm);
    }

    [Fact]
    public void Ctor_TrimsDisplayName()
    {
        var profile = new CustomerProfile(Id, UserId, "  Alice  ");

        profile.DisplayName.Should().Be("Alice");
    }

    [Fact]
    public void Ctor_EmptyId_Throws()
    {
        var act = () => new CustomerProfile(Guid.Empty, UserId, "Alice");
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Fact]
    public void Ctor_EmptyUserId_Throws()
    {
        var act = () => new CustomerProfile(Id, Guid.Empty, "Alice");
        act.Should().Throw<ArgumentException>().WithParameterName("userId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_BlankDisplayName_Throws(string name)
    {
        var act = () => new CustomerProfile(Id, UserId, name);
        act.Should().Throw<ArgumentException>().WithParameterName("displayName");
    }

    [Fact]
    public void Ctor_DisplayNameTooLong_Throws()
    {
        var name = new string('a', CustomerProfile.DisplayNameMaxLength + 1);
        var act = () => new CustomerProfile(Id, UserId, name);
        act.Should().Throw<ArgumentException>().WithParameterName("displayName");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(CustomerProfile.MaxSearchRadiusKm + 1)]
    public void Ctor_RadiusOutOfRange_Throws(int radius)
    {
        var act = () => new CustomerProfile(Id, UserId, "Alice", preferredSearchRadiusKm: radius);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("preferredSearchRadiusKm");
    }
}
