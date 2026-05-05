using Needlr.Domain.Portfolio;

namespace Needlr.Application.Abstractions.Persistence;

public interface ISessionPhotoRepository
{
    Task<SessionPhoto?> GetByIdAsync(Guid photoId, CancellationToken cancellationToken = default);

    /// <summary>Loads a photo with the parent <see cref="PortfolioPiece"/> eagerly loaded
    /// — used for ownership checks (does the piece belong to the calling artist?).</summary>
    Task<(SessionPhoto Photo, PortfolioPiece Piece)?> GetByIdWithPieceAsync(
        Guid photoId, CancellationToken cancellationToken = default);

    void Add(SessionPhoto photo);
}
