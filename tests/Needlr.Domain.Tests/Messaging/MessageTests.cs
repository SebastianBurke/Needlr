using FluentAssertions;
using Needlr.Domain.Messaging;
using Xunit;

namespace Needlr.Domain.Tests.Messaging;

public class MessageTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid ThreadId = Guid.NewGuid();
    private static readonly Guid SenderId = Guid.NewGuid();
    private static readonly DateTime SentAt = DateTime.UtcNow;

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var m = new Message(Id, ThreadId, SenderId, "Hello", SentAt);

        m.Id.Should().Be(Id);
        m.ThreadId.Should().Be(ThreadId);
        m.SenderId.Should().Be(SenderId);
        m.Body.Should().Be("Hello");
        m.SentAt.Should().Be(SentAt);
        m.IsReportedFlag.Should().BeFalse();
        m.ReadAt.Should().BeNull();
    }

    [Fact]
    public void Ctor_EmptySenderId_Throws()
    {
        var act = () => new Message(Id, ThreadId, Guid.Empty, "x", SentAt);
        act.Should().Throw<ArgumentException>().WithParameterName("senderId");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_BlankBody_Throws(string body)
    {
        var act = () => new Message(Id, ThreadId, SenderId, body, SentAt);
        act.Should().Throw<ArgumentException>().WithParameterName("body");
    }

    [Fact]
    public void Ctor_BodyTooLong_Throws()
    {
        var body = new string('a', Message.BodyMaxLength + 1);
        var act = () => new Message(Id, ThreadId, SenderId, body, SentAt);
        act.Should().Throw<ArgumentException>().WithParameterName("body");
    }

    [Fact]
    public void Ctor_NonUtcSentAt_Throws()
    {
        var local = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var act = () => new Message(Id, ThreadId, SenderId, "x", local);
        act.Should().Throw<ArgumentException>().WithParameterName("sentAt");
    }
}
