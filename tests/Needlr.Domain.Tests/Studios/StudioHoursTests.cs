using FluentAssertions;
using Needlr.Domain.Studios;
using Xunit;

namespace Needlr.Domain.Tests.Studios;

public class StudioHoursTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid StudioId = Guid.NewGuid();

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var hours = new StudioHours(Id, StudioId, DayOfWeek.Tuesday, new TimeOnly(10, 0), new TimeOnly(18, 0));

        hours.Id.Should().Be(Id);
        hours.StudioId.Should().Be(StudioId);
        hours.DayOfWeek.Should().Be(DayOfWeek.Tuesday);
        hours.OpenTime.Should().Be(new TimeOnly(10, 0));
        hours.CloseTime.Should().Be(new TimeOnly(18, 0));
        hours.IsClosed.Should().BeFalse();
    }

    [Fact]
    public void Ctor_IsClosed_AllowsAnyTimes()
    {
        var hours = new StudioHours(Id, StudioId, DayOfWeek.Sunday, new TimeOnly(0, 0), new TimeOnly(0, 0), isClosed: true);

        hours.IsClosed.Should().BeTrue();
    }

    [Fact]
    public void Ctor_EmptyId_Throws()
    {
        var act = () => new StudioHours(Guid.Empty, StudioId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0));
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Fact]
    public void Ctor_EmptyStudioId_Throws()
    {
        var act = () => new StudioHours(Id, Guid.Empty, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0));
        act.Should().Throw<ArgumentException>().WithParameterName("studioId");
    }

    [Fact]
    public void Ctor_CloseBeforeOrEqualOpen_WhenOpen_Throws()
    {
        var act = () => new StudioHours(Id, StudioId, DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(10, 0));
        act.Should().Throw<ArgumentException>();
    }
}
