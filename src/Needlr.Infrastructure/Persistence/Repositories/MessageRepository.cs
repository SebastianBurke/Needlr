using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Pagination;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;
using Needlr.Domain.Messaging;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class MessageRepository(NeedlrDbContext db) : IMessageRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<Message?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        _db.Messages.FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

    public Task<Message?> GetByIdWithAttachmentsAsync(Guid messageId, CancellationToken cancellationToken = default) =>
        _db.Messages
            .Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

    public async Task<PagedResult<Message>> ListByThreadAsync(
        Guid threadId, PageRequest page, CancellationToken cancellationToken = default)
    {
        var p = page.Clamp();
        var q = _db.Messages.Where(m => m.ThreadId == threadId);

        var total = await q.CountAsync(cancellationToken);
        var items = await q
            .OrderBy(m => m.SentAt)
            .Include(m => m.Attachments)
            .Skip(p.Skip).Take(p.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<Message>(items, p.Page, p.PageSize, total);
    }

    public Task<int> CountUnreadForUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _db.Messages.CountAsync(m =>
            m.ReadAt == null
            && m.SenderId != userId
            && _db.MessageThreads.Any(t => t.Id == m.ThreadId
                && t.Status == MessageThreadStatus.Active
                && _db.Bookings.Any(b => b.Id == t.BookingId
                    && (b.CustomerId == userId
                        || _db.Artists.Any(a => a.Id == b.ArtistId && a.UserId == userId)))),
            cancellationToken);

    public void Add(Message message) => _db.Messages.Add(message);
    public void AddAttachment(BookingAttachment attachment) => _db.BookingAttachments.Add(attachment);
}
