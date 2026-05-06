using Needlr.Application.Common.Pagination;
using Needlr.Domain.Messaging;

namespace Needlr.Application.Abstractions.Persistence;

public interface IMessageReportRepository
{
    Task<MessageReport?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken = default);

    /// <summary>Admin queue: unresolved reports first, oldest-reported-first within that group.</summary>
    Task<PagedResult<MessageReport>> ListPendingAsync(
        PageRequest page, CancellationToken cancellationToken = default);

    void Add(MessageReport report);
}
