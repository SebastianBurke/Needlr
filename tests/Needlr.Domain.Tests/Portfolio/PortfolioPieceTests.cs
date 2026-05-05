using FluentAssertions;
using Needlr.Domain.Enums;
using Needlr.Domain.Portfolio;
using Xunit;

namespace Needlr.Domain.Tests.Portfolio;

public class PortfolioPieceTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid ArtistId = Guid.NewGuid();
    private static readonly DateTime CreatedAt = DateTime.UtcNow;
    private const int YearCompleted = 2026;

    [Fact]
    public void Ctor_ValidArgs_AssignsAllProperties()
    {
        var piece = new PortfolioPiece(Id, ArtistId, BodyPlacement.Forearm, YearCompleted, CreatedAt, title: "Crow", description: "Linework crow");

        piece.Id.Should().Be(Id);
        piece.ArtistId.Should().Be(ArtistId);
        piece.BodyPlacement.Should().Be(BodyPlacement.Forearm);
        piece.YearCompleted.Should().Be(YearCompleted);
        piece.CreatedAt.Should().Be(CreatedAt);
        piece.Title.Should().Be("Crow");
        piece.Description.Should().Be("Linework crow");
        piece.ProgressionStatus.Should().Be(ProgressionStatus.SingleSession);
    }

    [Fact]
    public void Ctor_NonUtcCreatedAt_Throws()
    {
        var localTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var act = () => new PortfolioPiece(Id, ArtistId, BodyPlacement.Forearm, YearCompleted, localTime);
        act.Should().Throw<ArgumentException>().WithParameterName("createdAt");
    }

    [Fact]
    public void Ctor_EmptyArtistId_Throws()
    {
        var act = () => new PortfolioPiece(Id, Guid.Empty, BodyPlacement.Forearm, YearCompleted, CreatedAt);
        act.Should().Throw<ArgumentException>().WithParameterName("artistId");
    }

    [Fact]
    public void Ctor_TitleTooLong_Throws()
    {
        var title = new string('a', PortfolioPiece.TitleMaxLength + 1);
        var act = () => new PortfolioPiece(Id, ArtistId, BodyPlacement.Forearm, YearCompleted, CreatedAt, title: title);
        act.Should().Throw<ArgumentException>().WithParameterName("title");
    }

    [Fact]
    public void Ctor_DescriptionTooLong_Throws()
    {
        var desc = new string('a', PortfolioPiece.DescriptionMaxLength + 1);
        var act = () => new PortfolioPiece(Id, ArtistId, BodyPlacement.Forearm, YearCompleted, CreatedAt, description: desc);
        act.Should().Throw<ArgumentException>().WithParameterName("description");
    }

    [Fact]
    public void Ctor_NegativeApproximateSize_Throws()
    {
        var act = () => new PortfolioPiece(Id, ArtistId, BodyPlacement.Forearm, YearCompleted, CreatedAt, approximateSizeCm: -1);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("approximateSizeCm");
    }

    [Fact]
    public void Ctor_YearTooOld_Throws()
    {
        var act = () => new PortfolioPiece(Id, ArtistId, BodyPlacement.Forearm, PortfolioPiece.MinYearCompleted - 1, CreatedAt);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("yearCompleted");
    }

    [Fact]
    public void Ctor_YearInFuture_Throws()
    {
        var act = () => new PortfolioPiece(Id, ArtistId, BodyPlacement.Forearm, DateTime.UtcNow.Year + 2, CreatedAt);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("yearCompleted");
    }
}
