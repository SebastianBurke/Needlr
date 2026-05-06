using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.MessageThreads.ResolveMessageReport;

internal sealed class ResolveMessageReportCommandHandler(
    ICurrentUser currentUser,
    IMessageReportRepository reports,
    IClock clock) : IRequestHandler<ResolveMessageReportCommand, Result>
{
    public async Task<Result> Handle(ResolveMessageReportCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(UserRole.Admin) || currentUser.UserId is null)
            return Result.Failure(Error.Forbidden("Admin only."));

        var report = await reports.GetByIdAsync(request.ReportId, cancellationToken);
        if (report is null)
            return Result.Failure(Error.NotFound("Report"));
        if (report.ResolvedAt is not null)
            return Result.Failure(Error.FailedPrecondition("Report is already resolved."));

        report.Resolution = request.Resolution;
        report.ResolvedAt = clock.UtcNow;
        report.ResolvedByAdminId = currentUser.UserId.Value;
        return Result.Success();
    }
}
