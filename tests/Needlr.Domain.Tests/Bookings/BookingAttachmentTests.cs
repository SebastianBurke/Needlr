using FluentAssertions;
using Needlr.Domain.Bookings;
using Xunit;

namespace Needlr.Domain.Tests.Bookings;

public class BookingAttachmentTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid BookingId = Guid.NewGuid();
    private static readonly Guid MessageId = Guid.NewGuid();
    private static readonly Guid UploadedBy = Guid.NewGuid();
    private static readonly DateTime UploadedAt = DateTime.UtcNow;

    [Fact]
    public void Ctor_BookingScopedAttachment_Succeeds()
    {
        var att = new BookingAttachment(Id, BookingId, null, "https://x/a.jpg", "a.jpg", "image/jpeg", 1024, UploadedBy, UploadedAt);

        att.BookingId.Should().Be(BookingId);
        att.MessageId.Should().BeNull();
    }

    [Fact]
    public void Ctor_MessageScopedAttachment_Succeeds()
    {
        var att = new BookingAttachment(Id, null, MessageId, "https://x/a.jpg", "a.jpg", "image/jpeg", 1024, UploadedBy, UploadedAt);

        att.BookingId.Should().BeNull();
        att.MessageId.Should().Be(MessageId);
    }

    [Fact]
    public void Ctor_BothBookingAndMessageId_Throws()
    {
        var act = () => new BookingAttachment(Id, BookingId, MessageId, "u", "n", "m", 1, UploadedBy, UploadedAt);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Ctor_NeitherBookingNorMessageId_Throws()
    {
        var act = () => new BookingAttachment(Id, null, null, "u", "n", "m", 1, UploadedBy, UploadedAt);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_BlankUrl_Throws(string url)
    {
        var act = () => new BookingAttachment(Id, BookingId, null, url, "n", "m", 1, UploadedBy, UploadedAt);
        act.Should().Throw<ArgumentException>().WithParameterName("url");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(BookingAttachment.MaxSizeBytes + 1)]
    public void Ctor_SizeOutOfRange_Throws(long size)
    {
        var act = () => new BookingAttachment(Id, BookingId, null, "u", "n", "m", size, UploadedBy, UploadedAt);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("sizeBytes");
    }

    [Fact]
    public void Ctor_NonUtcUploadedAt_Throws()
    {
        var local = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var act = () => new BookingAttachment(Id, BookingId, null, "u", "n", "m", 1, UploadedBy, local);
        act.Should().Throw<ArgumentException>().WithParameterName("uploadedAt");
    }
}
