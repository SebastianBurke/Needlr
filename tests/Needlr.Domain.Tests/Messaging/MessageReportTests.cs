using FluentAssertions;
using Needlr.Domain.Enums;
using Needlr.Domain.Messaging;
using Xunit;

namespace Needlr.Domain.Tests.Messaging;

public class MessageReportTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid MessageId = Guid.NewGuid();
    private static readonly Guid ReportedBy = Guid.NewGuid();
    private static readonly DateTime ReportedAt = DateTime.UtcNow;

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var r = new MessageReport(Id, MessageId, ReportedBy, MessageReportReason.Harassment, ReportedAt, "Not nice.");

        r.Id.Should().Be(Id);
        r.MessageId.Should().Be(MessageId);
        r.ReportedByUserId.Should().Be(ReportedBy);
        r.Reason.Should().Be(MessageReportReason.Harassment);
        r.ReportedAt.Should().Be(ReportedAt);
        r.Note.Should().Be("Not nice.");
        r.Resolution.Should().BeNull();
    }

    [Fact]
    public void Ctor_EmptyMessageId_Throws()
    {
        var act = () => new MessageReport(Id, Guid.Empty, ReportedBy, MessageReportReason.Spam, ReportedAt);
        act.Should().Throw<ArgumentException>().WithParameterName("messageId");
    }

    [Fact]
    public void Ctor_EmptyReportedById_Throws()
    {
        var act = () => new MessageReport(Id, MessageId, Guid.Empty, MessageReportReason.Spam, ReportedAt);
        act.Should().Throw<ArgumentException>().WithParameterName("reportedByUserId");
    }

    [Fact]
    public void Ctor_NoteTooLong_Throws()
    {
        var note = new string('a', MessageReport.NoteMaxLength + 1);
        var act = () => new MessageReport(Id, MessageId, ReportedBy, MessageReportReason.Other, ReportedAt, note);
        act.Should().Throw<ArgumentException>().WithParameterName("note");
    }

    [Fact]
    public void Ctor_NonUtcReportedAt_Throws()
    {
        var local = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var act = () => new MessageReport(Id, MessageId, ReportedBy, MessageReportReason.Spam, local);
        act.Should().Throw<ArgumentException>().WithParameterName("reportedAt");
    }
}
