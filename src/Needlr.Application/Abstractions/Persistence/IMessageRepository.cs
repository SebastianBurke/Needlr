using Needlr.Application.Common.Pagination;
using Needlr.Domain.Bookings;
using Needlr.Domain.Messaging;

namespace Needlr.Application.Abstractions.Persistence;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>Eagerly loads <c>Attachments</c> for the messaging detail UI.</summary>
    Task<Message?> GetByIdWithAttachmentsAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Paginated messages for a thread, oldest-first so the UI scrolls naturally. Loads
    /// attachments eagerly because the typical render shows them inline.
    /// </summary>
    Task<PagedResult<Message>> ListByThreadAsync(
        Guid threadId, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count of unread messages across all of <paramref name="userId"/>'s active threads
    /// (messages they didn't author and haven't marked read).
    /// </summary>
    Task<int> CountUnreadForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    void Add(Message message);
    void AddAttachment(BookingAttachment attachment);
}
