using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Messaging;

namespace Needlr.Application.MessageThreads.ReportMessage;

internal sealed class ReportMessageCommandHandler(
    ICurrentUser currentUser,
    IMessageRepository messages,
    IMessageThreadRepository threads,
    IMessageReportRepository reports,
    IArtistRepository artists,
    IClock clock) : IRequestHandler<ReportMessageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ReportMessageCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<Guid>.Failure(Error.Unauthorized());
        var userId = currentUser.UserId.Value;

        var message = await messages.GetByIdAsync(request.MessageId, cancellationToken);
        if (message is null)
            return Result<Guid>.Failure(Error.NotFound("Message"));

        var pair = await threads.GetWithBookingAsync(message.ThreadId, cancellationToken);
        if (pair is null)
            return Result<Guid>.Failure(Error.NotFound("Thread"));

        var role = await ThreadParty.ResolveAsync(userId, pair.Value.Booking, artists, cancellationToken);
        if (role is null)
            return Result<Guid>.Failure(Error.Forbidden("Not a party to this thread."));

        message.IsReportedFlag = true;
        var report = new MessageReport(
            id: Guid.NewGuid(),
            messageId: message.Id,
            reportedByUserId: userId,
            reason: request.Reason,
            reportedAt: clock.UtcNow,
            note: request.Note);
        reports.Add(report);
        return Result<Guid>.Success(report.Id);
    }
}
