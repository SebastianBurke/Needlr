using FluentAssertions;
using Needlr.Domain.Availability;
using Xunit;

namespace Needlr.Domain.Tests.Availability;

public class BookingWindowTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid ArtistId = Guid.NewGuid();
    private static readonly DateTime Opens = DateTime.UtcNow;
    private static readonly DateTime Closes = DateTime.UtcNow.AddDays(7);
    private static readonly DateOnly RangeStart = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));
    private static readonly DateOnly RangeEnd = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60));

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var w = new BookingWindow(Id, ArtistId, Opens, Closes, RangeStart, RangeEnd);

        w.Id.Should().Be(Id);
        w.ArtistId.Should().Be(ArtistId);
        w.WindowOpensAt.Should().Be(Opens);
        w.WindowClosesAt.Should().Be(Closes);
        w.TargetRangeStart.Should().Be(RangeStart);
        w.TargetRangeEnd.Should().Be(RangeEnd);
    }

    [Fact]
    public void Ctor_NonUtcOpens_Throws()
    {
        var local = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var act = () => new BookingWindow(Id, ArtistId, local, Closes, RangeStart, RangeEnd);
        act.Should().Throw<ArgumentException>().WithParameterName("windowOpensAt");
    }

    [Fact]
    public void Ctor_NonUtcCloses_Throws()
    {
        var local = DateTime.SpecifyKind(DateTime.Now.AddDays(7), DateTimeKind.Local);
        var act = () => new BookingWindow(Id, ArtistId, Opens, local, RangeStart, RangeEnd);
        act.Should().Throw<ArgumentException>().WithParameterName("windowClosesAt");
    }

    [Fact]
    public void Ctor_ClosesNotAfterOpens_Throws()
    {
        var act = () => new BookingWindow(Id, ArtistId, Opens, Opens, RangeStart, RangeEnd);
        act.Should().Throw<ArgumentException>().WithParameterName("windowClosesAt");
    }

    [Fact]
    public void Ctor_RangeEndBeforeStart_Throws()
    {
        var act = () => new BookingWindow(Id, ArtistId, Opens, Closes, RangeEnd, RangeStart);
        act.Should().Throw<ArgumentException>().WithParameterName("targetRangeEnd");
    }
}
