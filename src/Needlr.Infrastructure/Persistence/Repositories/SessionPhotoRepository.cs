using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Portfolio;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class SessionPhotoRepository(NeedlrDbContext db) : ISessionPhotoRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<SessionPhoto?> GetByIdAsync(Guid photoId, CancellationToken cancellationToken = default) =>
        _db.SessionPhotos.FirstOrDefaultAsync(p => p.Id == photoId, cancellationToken);

    public async Task<(SessionPhoto Photo, PortfolioPiece Piece)?> GetByIdWithPieceAsync(
        Guid photoId, CancellationToken cancellationToken = default)
    {
        var row = await (
            from photo in _db.SessionPhotos
            join piece in _db.PortfolioPieces on photo.PortfolioPieceId equals piece.Id
            where photo.Id == photoId
            select new { Photo = photo, Piece = piece }
        ).FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : (row.Photo, row.Piece);
    }

    public void Add(SessionPhoto photo) => _db.SessionPhotos.Add(photo);
}
