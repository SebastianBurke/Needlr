using FluentAssertions;
using Needlr.Domain.Enums;
using Needlr.Domain.Portfolio;
using Xunit;

namespace Needlr.Domain.Tests.Portfolio;

public class SessionPhotoTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid PieceId = Guid.NewGuid();
    private static readonly Guid UploadedBy = Guid.NewGuid();
    private static readonly DateTime UploadedAt = DateTime.UtcNow;

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var photo = new SessionPhoto(Id, PieceId, order: 0, PhotoType.Fresh, "https://x/y.jpg", UploadedBy, UploadedByRole.Artist, UploadedAt);

        photo.Id.Should().Be(Id);
        photo.PortfolioPieceId.Should().Be(PieceId);
        photo.Order.Should().Be(0);
        photo.PhotoType.Should().Be(PhotoType.Fresh);
        photo.ImageUrl.Should().Be("https://x/y.jpg");
        photo.UploadedByUserId.Should().Be(UploadedBy);
        photo.UploadedByRole.Should().Be(UploadedByRole.Artist);
        photo.UploadedAt.Should().Be(UploadedAt);
        photo.IsHidden.Should().BeFalse();
    }

    [Fact]
    public void Ctor_NegativeOrder_Throws()
    {
        var act = () => new SessionPhoto(Id, PieceId, -1, PhotoType.Fresh, "u", UploadedBy, UploadedByRole.Artist, UploadedAt);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("order");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_BlankImageUrl_Throws(string url)
    {
        var act = () => new SessionPhoto(Id, PieceId, 0, PhotoType.Fresh, url, UploadedBy, UploadedByRole.Artist, UploadedAt);
        act.Should().Throw<ArgumentException>().WithParameterName("imageUrl");
    }

    [Fact]
    public void Ctor_EmptyUploadedByUserId_Throws()
    {
        var act = () => new SessionPhoto(Id, PieceId, 0, PhotoType.Fresh, "u", Guid.Empty, UploadedByRole.Artist, UploadedAt);
        act.Should().Throw<ArgumentException>().WithParameterName("uploadedByUserId");
    }

    [Fact]
    public void Ctor_NonUtcUploadedAt_Throws()
    {
        var local = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var act = () => new SessionPhoto(Id, PieceId, 0, PhotoType.Fresh, "u", UploadedBy, UploadedByRole.Artist, local);
        act.Should().Throw<ArgumentException>().WithParameterName("uploadedAt");
    }
}
