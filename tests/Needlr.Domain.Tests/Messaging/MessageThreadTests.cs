using FluentAssertions;
using Needlr.Domain.Enums;
using Needlr.Domain.Messaging;
using Xunit;

namespace Needlr.Domain.Tests.Messaging;

public class MessageThreadTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid BookingId = Guid.NewGuid();
    private static readonly DateTime OpenedAt = DateTime.UtcNow;

    [Fact]
    public void Ctor_ValidArgs_DefaultsToActive()
    {
        var t = new MessageThread(Id, BookingId, OpenedAt);

        t.Id.Should().Be(Id);
        t.BookingId.Should().Be(BookingId);
        t.OpenedAt.Should().Be(OpenedAt);
        t.Status.Should().Be(MessageThreadStatus.Active);
        t.LockedAt.Should().BeNull();
        t.Messages.Should().BeEmpty();
    }

    [Fact]
    public void Ctor_EmptyId_Throws()
    {
        var act = () => new MessageThread(Guid.Empty, BookingId, OpenedAt);
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Fact]
    public void Ctor_EmptyBookingId_Throws()
    {
        var act = () => new MessageThread(Id, Guid.Empty, OpenedAt);
        act.Should().Throw<ArgumentException>().WithParameterName("bookingId");
    }

    [Fact]
    public void Ctor_NonUtcOpenedAt_Throws()
    {
        var local = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var act = () => new MessageThread(Id, BookingId, local);
        act.Should().Throw<ArgumentException>().WithParameterName("openedAt");
    }
}
