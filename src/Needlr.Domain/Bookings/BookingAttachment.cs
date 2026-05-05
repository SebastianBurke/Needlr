namespace Needlr.Domain.Bookings;

/// <summary>
/// Dual-use attachment entity. Either <see cref="BookingId"/> or <see cref="MessageId"/> is set
/// (never both, never neither) — the same row models a booking-request attachment or an in-thread
/// message attachment. Per ADR-003, blob storage is purged 1 year after the related booking
/// reaches a terminal state; <see cref="Url"/> is cleared while the row is retained.
/// </summary>
public sealed class BookingAttachment
{
    public const int OriginalFilenameMaxLength = 500;
    public const int MimeTypeMaxLength = 200;
    public const long MaxSizeBytes = 10L * 1024 * 1024;  // 10 MB

    public Guid Id { get; init; }
    public Guid? BookingId { get; init; }
    public Guid? MessageId { get; init; }
    public string? Url { get; set; }
    public string OriginalFilename { get; set; } = null!;
    public string MimeType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public Guid UploadedByUserId { get; init; }
    public DateTime UploadedAt { get; init; }

    private BookingAttachment() { }

    public BookingAttachment(
        Guid id,
        Guid? bookingId,
        Guid? messageId,
        string url,
        string originalFilename,
        string mimeType,
        long sizeBytes,
        Guid uploadedByUserId,
        DateTime uploadedAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (bookingId is null == messageId is null)
            throw new ArgumentException(
                "Exactly one of BookingId or MessageId must be set.",
                bookingId is null ? nameof(bookingId) : nameof(messageId));
        if (bookingId is { } b && b == Guid.Empty)
            throw new ArgumentException("BookingId, when set, must not be empty.", nameof(bookingId));
        if (messageId is { } m && m == Guid.Empty)
            throw new ArgumentException("MessageId, when set, must not be empty.", nameof(messageId));

        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFilename);
        if (originalFilename.Length > OriginalFilenameMaxLength)
            throw new ArgumentException($"OriginalFilename must be <= {OriginalFilenameMaxLength} chars.", nameof(originalFilename));
        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        if (mimeType.Length > MimeTypeMaxLength)
            throw new ArgumentException($"MimeType must be <= {MimeTypeMaxLength} chars.", nameof(mimeType));
        if (sizeBytes <= 0 || sizeBytes > MaxSizeBytes)
            throw new ArgumentOutOfRangeException(nameof(sizeBytes),
                $"Must be in (0, {MaxSizeBytes}] bytes.");
        if (uploadedByUserId == Guid.Empty)
            throw new ArgumentException("UploadedByUserId is required.", nameof(uploadedByUserId));
        if (uploadedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("UploadedAt must be UTC.", nameof(uploadedAt));

        Id = id;
        BookingId = bookingId;
        MessageId = messageId;
        Url = url;
        OriginalFilename = originalFilename;
        MimeType = mimeType;
        SizeBytes = sizeBytes;
        UploadedByUserId = uploadedByUserId;
        UploadedAt = uploadedAt;
    }
}
