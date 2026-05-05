using Needlr.Domain.Portfolio;

namespace Needlr.Application.Abstractions.Persistence;

public interface IPortfolioPieceRepository
{
    Task<PortfolioPiece?> GetByIdAsync(Guid pieceId, CancellationToken cancellationToken = default);

    /// <summary>Loads a piece with its session photos eagerly loaded.</summary>
    Task<PortfolioPiece?> GetByIdWithPhotosAsync(Guid pieceId, CancellationToken cancellationToken = default);

    /// <summary>Loads a piece with photos AND tattoo styles eagerly loaded.</summary>
    Task<PortfolioPiece?> GetByIdWithDetailsAsync(Guid pieceId, CancellationToken cancellationToken = default);

    /// <summary>Returns the piece linked to a Needlr booking (used by healed-photo flow).</summary>
    Task<PortfolioPiece?> FindByLinkedBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PortfolioPiece>> ListByArtistAsync(
        Guid artistId, int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountByArtistAsync(Guid artistId, CancellationToken cancellationToken = default);

    /// <summary>Pieces by all currently-active artists at the studio (Permanent + GuestSpot).</summary>
    Task<IReadOnlyList<PortfolioPiece>> ListByStudioAsync(
        Guid studioId, int skip, int take, CancellationToken cancellationToken = default);

    Task<int> CountByStudioAsync(Guid studioId, CancellationToken cancellationToken = default);

    void Add(PortfolioPiece piece);
    void Remove(PortfolioPiece piece);
}
