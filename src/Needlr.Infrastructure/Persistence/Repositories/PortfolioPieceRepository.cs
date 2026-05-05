using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Enums;
using Needlr.Domain.Portfolio;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class PortfolioPieceRepository(NeedlrDbContext db) : IPortfolioPieceRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<PortfolioPiece?> GetByIdAsync(Guid pieceId, CancellationToken cancellationToken = default) =>
        _db.PortfolioPieces.FirstOrDefaultAsync(p => p.Id == pieceId, cancellationToken);

    public Task<PortfolioPiece?> GetByIdWithPhotosAsync(Guid pieceId, CancellationToken cancellationToken = default) =>
        _db.PortfolioPieces
            .Include(p => p.Sessions.OrderBy(s => s.Order))
            .FirstOrDefaultAsync(p => p.Id == pieceId, cancellationToken);

    public Task<PortfolioPiece?> GetByIdWithDetailsAsync(Guid pieceId, CancellationToken cancellationToken = default) =>
        _db.PortfolioPieces
            .Include(p => p.Sessions.OrderBy(s => s.Order))
            .Include(p => p.Styles)
            .FirstOrDefaultAsync(p => p.Id == pieceId, cancellationToken);

    public Task<PortfolioPiece?> FindByLinkedBookingAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        _db.PortfolioPieces
            .Include(p => p.Sessions)
            .FirstOrDefaultAsync(p => p.LinkedBookingId == bookingId, cancellationToken);

    public async Task<IReadOnlyList<PortfolioPiece>> ListByArtistAsync(
        Guid artistId, int skip, int take, CancellationToken cancellationToken = default) =>
        await _db.PortfolioPieces
            .Where(p => p.ArtistId == artistId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip).Take(take)
            .Include(p => p.Sessions.OrderBy(s => s.Order))
            .ToListAsync(cancellationToken);

    public Task<int> CountByArtistAsync(Guid artistId, CancellationToken cancellationToken = default) =>
        _db.PortfolioPieces.CountAsync(p => p.ArtistId == artistId, cancellationToken);

    public async Task<IReadOnlyList<PortfolioPiece>> ListByStudioAsync(
        Guid studioId, int skip, int take, CancellationToken cancellationToken = default) =>
        await _db.PortfolioPieces
            .Where(p => _db.ArtistStudioAffiliations.Any(a =>
                a.ArtistId == p.ArtistId
                && a.StudioId == studioId
                && a.Status == AffiliationStatus.Active))
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip).Take(take)
            .Include(p => p.Sessions.OrderBy(s => s.Order))
            .ToListAsync(cancellationToken);

    public Task<int> CountByStudioAsync(Guid studioId, CancellationToken cancellationToken = default) =>
        _db.PortfolioPieces.CountAsync(p =>
            _db.ArtistStudioAffiliations.Any(a =>
                a.ArtistId == p.ArtistId
                && a.StudioId == studioId
                && a.Status == AffiliationStatus.Active),
            cancellationToken);

    public void Add(PortfolioPiece piece) => _db.PortfolioPieces.Add(piece);

    public void Remove(PortfolioPiece piece) => _db.PortfolioPieces.Remove(piece);
}
