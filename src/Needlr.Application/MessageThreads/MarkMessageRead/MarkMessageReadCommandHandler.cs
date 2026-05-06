using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.MessageThreads.MarkMessageRead;

internal sealed class MarkMessageReadCommandHandler(
    ICurrentUser currentUser,
    IMessageRepository messages,
    IMessageThreadRepository threads,
    IArtistRepository artists,
    IClock clock) : IRequestHandler<MarkMessageReadCommand, Result>
{
    public async Task<Result> Handle(MarkMessageReadCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized());
        var userId = currentUser.UserId.Value;

        var message = await messages.GetByIdAsync(request.MessageId, cancellationToken);
        if (message is null)
            return Result.Failure(Error.NotFound("Message"));

        // The sender can't be the reader. The reader must be the other party on the booking.
        if (message.SenderId == userId)
            return Result.Failure(Error.FailedPrecondition("Senders don't mark their own messages read."));

        var pair = await threads.GetWithBookingAsync(message.ThreadId, cancellationToken);
        if (pair is null)
            return Result.Failure(Error.NotFound("Thread"));
        var role = await ThreadParty.ResolveAsync(userId, pair.Value.Booking, artists, cancellationToken);
        if (role is null)
            return Result.Failure(Error.Forbidden("Not a party to this thread."));

        message.ReadAt ??= clock.UtcNow;
        return Result.Success();
    }
}
