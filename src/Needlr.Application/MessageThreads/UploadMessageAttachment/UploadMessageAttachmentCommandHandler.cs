using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;

namespace Needlr.Application.MessageThreads.UploadMessageAttachment;

internal sealed class UploadMessageAttachmentCommandHandler(
    ICurrentUser currentUser,
    IMessageRepository messages,
    IMessageThreadRepository threads,
    IImageStorage imageStorage,
    IClock clock) : IRequestHandler<UploadMessageAttachmentCommand, Result<Guid>>
{
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public async Task<Result<Guid>> Handle(UploadMessageAttachmentCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<Guid>.Failure(Error.Unauthorized());
        var userId = currentUser.UserId.Value;

        if (!AllowedMimeTypes.Contains(request.ContentType))
            return Result<Guid>.Failure(Error.Validation("Unsupported content type."));
        if (request.SizeBytes <= 0 || request.SizeBytes > BookingAttachment.MaxSizeBytes)
            return Result<Guid>.Failure(Error.Validation(
                $"Attachment size must be in (0, {BookingAttachment.MaxSizeBytes}] bytes."));

        var message = await messages.GetByIdAsync(request.MessageId, cancellationToken);
        if (message is null)
            return Result<Guid>.Failure(Error.NotFound("Message"));

        if (message.SenderId != userId)
            return Result<Guid>.Failure(Error.Forbidden("Only the sender can attach files."));

        var thread = await threads.GetByIdAsync(message.ThreadId, cancellationToken);
        if (thread is null)
            return Result<Guid>.Failure(Error.NotFound("Thread"));
        if (thread.Status != MessageThreadStatus.Active)
            return Result<Guid>.Failure(Error.FailedPrecondition("Thread is locked."));

        var key = await imageStorage.UploadAsync(
            request.FileContent, request.ContentType,
            keyPrefix: $"messages/{message.Id:N}", cancellationToken);

        var attachment = new BookingAttachment(
            id: Guid.NewGuid(),
            bookingId: null,
            messageId: message.Id,
            url: key,
            originalFilename: request.OriginalFilename,
            mimeType: request.ContentType,
            sizeBytes: request.SizeBytes,
            uploadedByUserId: userId,
            uploadedAt: clock.UtcNow);
        messages.AddAttachment(attachment);
        return Result<Guid>.Success(attachment.Id);
    }
}
