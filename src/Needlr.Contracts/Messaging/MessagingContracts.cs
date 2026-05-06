namespace Needlr.Contracts.Messaging;

// ---- Requests ----

public sealed record SendMessageRequest(string Body);

public sealed record ReportMessageRequest(string Reason, string? Note);

public sealed record HideMessageRequest(string Reason);

public sealed record ResolveReportRequest(string Resolution);

// ---- Responses ----

public sealed record MessageAttachmentResponse(
    Guid Id,
    string? Url,
    string OriginalFilename,
    string MimeType,
    long SizeBytes,
    DateTime UploadedAt);

public sealed record MessageResponse(
    Guid Id,
    Guid ThreadId,
    Guid SenderId,
    string Body,
    DateTime SentAt,
    DateTime? ReadAt,
    bool IsReportedFlag,
    IReadOnlyList<MessageAttachmentResponse> Attachments);

public sealed record MessagePageResponse(
    IReadOnlyList<MessageResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNext,
    bool HasPrevious);

public sealed record ThreadResponse(
    Guid Id,
    Guid BookingId,
    DateTime OpenedAt,
    DateTime? LockedAt,
    string Status);

public sealed record ThreadPageResponse(
    IReadOnlyList<ThreadResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNext,
    bool HasPrevious);

public sealed record UnreadCountResponse(int Count);

public sealed record MessageReportResponse(
    Guid Id,
    Guid MessageId,
    Guid ReportedByUserId,
    string Reason,
    string? Note,
    DateTime ReportedAt,
    DateTime? ResolvedAt,
    Guid? ResolvedByAdminId,
    string? Resolution);

public sealed record MessageReportPageResponse(
    IReadOnlyList<MessageReportResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNext,
    bool HasPrevious);
