using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;
using Needlr.Domain.Messaging;

namespace Needlr.Application.MessageThreads.SendMessage;

internal sealed class SendMessageCommandHandler(
    ICurrentUser currentUser,
    IMessageThreadRepository threads,
    IMessageRepository messages,
    IArtistRepository artists,
    IClock clock) : IRequestHandler<SendMessageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<Guid>.Failure(Error.Unauthorized());
        var userId = currentUser.UserId.Value;

        var pair = await threads.GetWithBookingAsync(request.ThreadId, cancellationToken);
        if (pair is null)
            return Result<Guid>.Failure(Error.NotFound("Thread"));
        var (thread, booking) = pair.Value;

        if (thread.Status != MessageThreadStatus.Active)
            return Result<Guid>.Failure(Error.FailedPrecondition("Thread is locked."));

        var role = await ThreadParty.ResolveAsync(userId, booking, artists, cancellationToken);
        if (role is null)
            return Result<Guid>.Failure(Error.Forbidden("Not a party to this thread."));

        var message = new Message(
            id: Guid.NewGuid(),
            threadId: thread.Id,
            senderId: userId,
            body: request.Body,
            sentAt: clock.UtcNow);
        messages.Add(message);
        return Result<Guid>.Success(message.Id);
    }
}
