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
    INotificationDispatcher notifications,
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

        // Notify the *other* party. Customer is on Booking.CustomerId; the artist's user id
        // comes from the artist row.
        var recipientUserId = role == ThreadParty.Role.Customer
            ? (await artists.GetByIdAsync(booking.ArtistId, cancellationToken))?.UserId
            : booking.CustomerId;
        if (recipientUserId is { } recipient && recipient != Guid.Empty)
        {
            await notifications.DispatchAsync(
                recipient,
                NotificationType.NewMessage,
                new NotificationContent(
                    EmailSubject: "New message on Needlr",
                    EmailBody: "You have a new message in your booking thread. Open Needlr to read it.",
                    PushTitle: "New message",
                    PushBody: "Tap to view"),
                cancellationToken);
        }

        return Result<Guid>.Success(message.Id);
    }
}
