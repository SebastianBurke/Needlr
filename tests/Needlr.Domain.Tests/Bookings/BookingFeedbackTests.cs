using FluentAssertions;
using Needlr.Domain.Bookings;
using Xunit;

namespace Needlr.Domain.Tests.Bookings;

public class BookingFeedbackTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid BookingId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly DateTime SubmittedAt = DateTime.UtcNow;

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var fb = new BookingFeedback(Id, BookingId, CustomerId, 5, 5, 4, true, SubmittedAt, "Loved it.");

        fb.CommunicationRating.Should().Be(5);
        fb.CleanlinessRating.Should().Be(5);
        fb.RespectedDesignBriefRating.Should().Be(4);
        fb.WouldBookAgain.Should().BeTrue();
        fb.FreeText.Should().Be("Loved it.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Ctor_CommunicationRating_OutOfRange_Throws(int r)
    {
        var act = () => new BookingFeedback(Id, BookingId, CustomerId, r, 3, 3, true, SubmittedAt);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("communicationRating");
    }

    [Fact]
    public void Ctor_CleanlinessRating_OutOfRange_Throws()
    {
        var act = () => new BookingFeedback(Id, BookingId, CustomerId, 3, 0, 3, true, SubmittedAt);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("cleanlinessRating");
    }

    [Fact]
    public void Ctor_RespectedDesignBriefRating_OutOfRange_Throws()
    {
        var act = () => new BookingFeedback(Id, BookingId, CustomerId, 3, 3, 6, true, SubmittedAt);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("respectedDesignBriefRating");
    }

    [Fact]
    public void Ctor_FreeTextTooLong_Throws()
    {
        var text = new string('a', BookingFeedback.FreeTextMaxLength + 1);
        var act = () => new BookingFeedback(Id, BookingId, CustomerId, 3, 3, 3, true, SubmittedAt, text);
        act.Should().Throw<ArgumentException>().WithParameterName("freeText");
    }

    [Fact]
    public void Ctor_NonUtcSubmittedAt_Throws()
    {
        var local = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var act = () => new BookingFeedback(Id, BookingId, CustomerId, 3, 3, 3, true, local);
        act.Should().Throw<ArgumentException>().WithParameterName("submittedAt");
    }
}
