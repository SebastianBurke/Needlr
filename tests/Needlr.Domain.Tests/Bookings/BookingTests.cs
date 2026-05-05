using FluentAssertions;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;
using Xunit;

namespace Needlr.Domain.Tests.Bookings;

public class BookingTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid ArtistId = Guid.NewGuid();
    private static readonly Guid StudioId = Guid.NewGuid();
    private static readonly DateTime RequestedAt = DateTime.UtcNow;
    private static readonly DateOnly RequestedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));

    private static Booking BuildValid() => new(
        Id, CustomerId, ArtistId, StudioId,
        BookingType.TattooSession, RequestedAt, RequestedDate,
        estimatedDurationHours: 3m,
        description: "A small crow on the forearm.",
        bodyPlacement: BodyPlacement.Forearm,
        depositAmountCad: 100m,
        cancellationPolicySnapshot: CancellationPolicy.Standard);

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var b = BuildValid();

        b.Id.Should().Be(Id);
        b.CustomerId.Should().Be(CustomerId);
        b.ArtistId.Should().Be(ArtistId);
        b.StudioId.Should().Be(StudioId);
        b.Status.Should().Be(BookingStatus.Requested);
        b.DepositAmountCad.Should().Be(100m);
        b.CancellationPolicySnapshot.Should().Be(CancellationPolicy.Standard);
        b.IsAttachmentsPurged.Should().BeFalse();
    }

    [Fact]
    public void Ctor_EmptyId_Throws()
    {
        var act = () => new Booking(Guid.Empty, CustomerId, ArtistId, StudioId, BookingType.TattooSession, RequestedAt, RequestedDate, 3m, "x", BodyPlacement.Forearm, 100m, CancellationPolicy.Standard);
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Fact]
    public void Ctor_EmptyCustomerId_Throws()
    {
        var act = () => new Booking(Id, Guid.Empty, ArtistId, StudioId, BookingType.TattooSession, RequestedAt, RequestedDate, 3m, "x", BodyPlacement.Forearm, 100m, CancellationPolicy.Standard);
        act.Should().Throw<ArgumentException>().WithParameterName("customerId");
    }

    [Fact]
    public void Ctor_EmptyArtistId_Throws()
    {
        var act = () => new Booking(Id, CustomerId, Guid.Empty, StudioId, BookingType.TattooSession, RequestedAt, RequestedDate, 3m, "x", BodyPlacement.Forearm, 100m, CancellationPolicy.Standard);
        act.Should().Throw<ArgumentException>().WithParameterName("artistId");
    }

    [Fact]
    public void Ctor_EmptyStudioId_Throws()
    {
        var act = () => new Booking(Id, CustomerId, ArtistId, Guid.Empty, BookingType.TattooSession, RequestedAt, RequestedDate, 3m, "x", BodyPlacement.Forearm, 100m, CancellationPolicy.Standard);
        act.Should().Throw<ArgumentException>().WithParameterName("studioId");
    }

    [Fact]
    public void Ctor_NonUtcRequestedAt_Throws()
    {
        var local = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var act = () => new Booking(Id, CustomerId, ArtistId, StudioId, BookingType.TattooSession, local, RequestedDate, 3m, "x", BodyPlacement.Forearm, 100m, CancellationPolicy.Standard);
        act.Should().Throw<ArgumentException>().WithParameterName("requestedAt");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_BlankDescription_Throws(string desc)
    {
        var act = () => new Booking(Id, CustomerId, ArtistId, StudioId, BookingType.TattooSession, RequestedAt, RequestedDate, 3m, desc, BodyPlacement.Forearm, 100m, CancellationPolicy.Standard);
        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void Ctor_DescriptionTooLong_Throws()
    {
        var desc = new string('a', Booking.DescriptionMaxLength + 1);
        var act = () => new Booking(Id, CustomerId, ArtistId, StudioId, BookingType.TattooSession, RequestedAt, RequestedDate, 3m, desc, BodyPlacement.Forearm, 100m, CancellationPolicy.Standard);
        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(25)]  // > MaxEstimatedDurationHours
    public void Ctor_DurationOutOfRange_Throws(double hours)
    {
        var act = () => new Booking(Id, CustomerId, ArtistId, StudioId, BookingType.TattooSession, RequestedAt, RequestedDate, (decimal)hours, "x", BodyPlacement.Forearm, 100m, CancellationPolicy.Standard);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("estimatedDurationHours");
    }

    [Fact]
    public void Ctor_DepositTooLow_Throws()
    {
        var act = () => new Booking(Id, CustomerId, ArtistId, StudioId, BookingType.TattooSession, RequestedAt, RequestedDate, 3m, "x", BodyPlacement.Forearm, 0m, CancellationPolicy.Standard);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("depositAmountCad");
    }

    [Fact]
    public void Ctor_NegativeApproximateSize_Throws()
    {
        var act = () => new Booking(Id, CustomerId, ArtistId, StudioId, BookingType.TattooSession, RequestedAt, RequestedDate, 3m, "x", BodyPlacement.Forearm, 100m, CancellationPolicy.Standard, approximateSizeCm: -1);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("approximateSizeCm");
    }
}
