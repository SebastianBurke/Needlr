using Needlr.Application.Messaging;

namespace Needlr.Application.MessageThreads.UploadMessageAttachment;

/// <summary>
/// Attaches a file to a message in an Active thread. The caller must be the message's
/// sender so customers can't tack files onto an artist's message and vice-versa. Size cap
/// is enforced both at the controller (Form size) and in the command (defense in depth).
/// </summary>
public sealed record UploadMessageAttachmentCommand(
    Guid MessageId,
    Stream FileContent,
    string ContentType,
    string OriginalFilename,
    long SizeBytes) : ICommand<Guid>;
