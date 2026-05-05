using Needlr.Domain.Enums;

namespace Needlr.Domain.Portfolio;

/// <summary>
/// A single photo on a portfolio piece. Multi-session pieces have an ordered collection
/// of these. Per FEATURE_SPECS.md § Customer-uploaded photo policy, an artist can only hide
/// a customer-uploaded photo for content-policy violations, not for being unflattering or
/// showing poor healing.
/// </summary>
public sealed class SessionPhoto
{
    public const int HiddenReasonMaxLength = 500;

    public Guid Id { get; init; }
    public Guid PortfolioPieceId { get; init; }
    public int Order { get; set; }
    public PhotoType PhotoType { get; init; }
    public string? ImageUrl { get; set; }
    public Guid UploadedByUserId { get; init; }
    public UploadedByRole UploadedByRole { get; init; }
    public DateTime UploadedAt { get; init; }
    public DateTime? LinkedSessionDate { get; set; }
    public bool IsHidden { get; set; }
    public string? HiddenReason { get; set; }

    private SessionPhoto() { }

    public SessionPhoto(
        Guid id,
        Guid portfolioPieceId,
        int order,
        PhotoType photoType,
        string imageUrl,
        Guid uploadedByUserId,
        UploadedByRole uploadedByRole,
        DateTime uploadedAt,
        DateTime? linkedSessionDate = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (portfolioPieceId == Guid.Empty) throw new ArgumentException("PortfolioPieceId is required.", nameof(portfolioPieceId));
        if (order < 0) throw new ArgumentOutOfRangeException(nameof(order), "Must be non-negative.");
        ArgumentException.ThrowIfNullOrWhiteSpace(imageUrl);
        if (uploadedByUserId == Guid.Empty)
            throw new ArgumentException("UploadedByUserId is required.", nameof(uploadedByUserId));
        if (uploadedAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("UploadedAt must be UTC.", nameof(uploadedAt));
        if (linkedSessionDate is { } d && d.Kind != DateTimeKind.Utc)
            throw new ArgumentException("LinkedSessionDate must be UTC.", nameof(linkedSessionDate));

        Id = id;
        PortfolioPieceId = portfolioPieceId;
        Order = order;
        PhotoType = photoType;
        ImageUrl = imageUrl;
        UploadedByUserId = uploadedByUserId;
        UploadedByRole = uploadedByRole;
        UploadedAt = uploadedAt;
        LinkedSessionDate = linkedSessionDate;
    }
}
