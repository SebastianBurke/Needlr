using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Pagination;
using Needlr.Domain.Messaging;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class MessageReportRepository(NeedlrDbContext db) : IMessageReportRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<MessageReport?> GetByIdAsync(Guid reportId, CancellationToken cancellationToken = default) =>
        _db.MessageReports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

    public async Task<PagedResult<MessageReport>> ListPendingAsync(
        PageRequest page, CancellationToken cancellationToken = default)
    {
        var p = page.Clamp();
        var q = _db.MessageReports.Where(r => r.ResolvedAt == null);
        var total = await q.CountAsync(cancellationToken);
        var rows = await q
            .OrderBy(r => r.ReportedAt)
            .Skip(p.Skip).Take(p.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<MessageReport>(rows, p.Page, p.PageSize, total);
    }

    public void Add(MessageReport report) => _db.MessageReports.Add(report);
}
