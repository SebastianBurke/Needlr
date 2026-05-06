using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Notifications;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class PushSubscriptionRepository(NeedlrDbContext db) : IPushSubscriptionRepository
{
    private readonly NeedlrDbContext _db = db;

    public async Task<IReadOnlyList<PushSubscription>> ListByUserAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await _db.PushSubscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<PushSubscription?> GetByEndpointAsync(
        Guid userId, string endpoint, CancellationToken cancellationToken = default) =>
        _db.PushSubscriptions.FirstOrDefaultAsync(
            s => s.UserId == userId && s.Endpoint == endpoint, cancellationToken);

    public Task<PushSubscription?> GetByIdAsync(
        Guid subscriptionId, CancellationToken cancellationToken = default) =>
        _db.PushSubscriptions.FirstOrDefaultAsync(s => s.Id == subscriptionId, cancellationToken);

    public void Add(PushSubscription subscription) =>
        _db.PushSubscriptions.Add(subscription);

    public void Remove(PushSubscription subscription) =>
        _db.PushSubscriptions.Remove(subscription);
}
