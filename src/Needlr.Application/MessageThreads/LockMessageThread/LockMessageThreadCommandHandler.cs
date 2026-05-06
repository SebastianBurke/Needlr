using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.MessageThreads.LockMessageThread;

internal sealed class LockMessageThreadCommandHandler(
    IMessageThreadRepository threads,
    IClock clock) : IRequestHandler<LockMessageThreadCommand, Result>
{
    public async Task<Result> Handle(LockMessageThreadCommand request, CancellationToken cancellationToken)
    {
        var thread = await threads.GetByBookingIdAsync(request.BookingId, cancellationToken);
        if (thread is null)
            return Result.Success(); // No thread for this booking — never reached DepositCaptured.
        if (thread.Status == MessageThreadStatus.Locked)
            return Result.Success(); // Already locked — idempotent.

        thread.Status = MessageThreadStatus.Locked;
        thread.LockedAt = clock.UtcNow;
        return Result.Success();
    }
}
