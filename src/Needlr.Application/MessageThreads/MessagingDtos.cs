using Needlr.Domain.Enums;

namespace Needlr.Application.MessageThreads;

public sealed record MessageDto(
    Guid Id,
    Guid ThreadId,
    Guid SenderId,
    string Body,
    DateTime SentAt,
    DateTime? ReadAt,
    bool IsReportedFlag,
    IReadOnlyList<MessageAttachmentDto> Attachments);

public sealed record MessageAttachmentDto(
    Guid Id,
    string? Url,
    string OriginalFilename,
    string MimeType,
    long SizeBytes,
    DateTime UploadedAt);

public sealed record ThreadDto(
    Guid Id,
    Guid BookingId,
    DateTime OpenedAt,
    DateTime? LockedAt,
    MessageThreadStatus Status,
    DateTime? LastMessageAt);

public sealed record MessageReportDto(
    Guid Id,
    Guid MessageId,
    Guid ReportedByUserId,
    MessageReportReason Reason,
    string? Note,
    DateTime ReportedAt,
    DateTime? ResolvedAt,
    Guid? ResolvedByAdminId,
    MessageReportResolution? Resolution);
