using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Identity;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class ArtistRepository(NeedlrDbContext db) : IArtistRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<Artist?> GetByIdAsync(Guid artistId, CancellationToken cancellationToken = default) =>
        _db.Artists.FirstOrDefaultAsync(a => a.Id == artistId, cancellationToken);

    public Task<Artist?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _db.Artists.FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

    public Task<bool> ExistsAsync(Guid artistId, CancellationToken cancellationToken = default) =>
        _db.Artists.AnyAsync(a => a.Id == artistId, cancellationToken);
}
