using Needlr.Domain.Enums;

namespace Needlr.Domain.Portfolio;

/// <summary>
/// One piece of an artist's portfolio. The unit of the piece-first portfolio model
/// (FEATURE_SPECS.md § Portfolio); both artist-grid and studio-collective views project over pieces.
/// </summary>
public sealed class PortfolioPiece
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 4000;
    public const int MaxFreeformTags = 3;
    public const int FreeformTagMaxLength = 50;
    public const int MinYearCompleted = 1900;

    public Guid Id { get; init; }
    public Guid ArtistId { get; init; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public BodyPlacement BodyPlacement { get; set; }
    public int? ApproximateSizeCm { get; set; }
    public decimal? EstimatedSessionLengthHours { get; set; }
    public int YearCompleted { get; set; }
    public ProgressionStatus ProgressionStatus { get; set; } = ProgressionStatus.SingleSession;
    public Guid? LinkedBookingId { get; init; }
    public DateTime CreatedAt { get; init; }

    public ICollection<TattooStyle> Styles { get; set; } = new List<TattooStyle>();
    public ICollection<string> FreeformTags { get; set; } = new List<string>();
    public ICollection<SessionPhoto> Sessions { get; set; } = new List<SessionPhoto>();

    private PortfolioPiece() { }

    public PortfolioPiece(
        Guid id,
        Guid artistId,
        BodyPlacement bodyPlacement,
        int yearCompleted,
        DateTime createdAt,
        string? title = null,
        string? description = null,
        int? approximateSizeCm = null,
        decimal? estimatedSessionLengthHours = null,
        ProgressionStatus progressionStatus = ProgressionStatus.SingleSession,
        Guid? linkedBookingId = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (artistId == Guid.Empty) throw new ArgumentException("ArtistId is required.", nameof(artistId));
        if (title is { Length: > TitleMaxLength })
            throw new ArgumentException($"Title must be <= {TitleMaxLength} chars.", nameof(title));
        if (description is { Length: > DescriptionMaxLength })
            throw new ArgumentException($"Description must be <= {DescriptionMaxLength} chars.", nameof(description));
        if (approximateSizeCm is < 0)
            throw new ArgumentOutOfRangeException(nameof(approximateSizeCm), "Must be non-negative.");
        if (estimatedSessionLengthHours is < 0)
            throw new ArgumentOutOfRangeException(nameof(estimatedSessionLengthHours), "Must be non-negative.");
        if (yearCompleted < MinYearCompleted || yearCompleted > DateTime.UtcNow.Year + 1)
            throw new ArgumentOutOfRangeException(nameof(yearCompleted),
                $"Must be in [{MinYearCompleted}, {DateTime.UtcNow.Year + 1}].");
        if (createdAt.Kind != DateTimeKind.Utc)
            throw new ArgumentException("CreatedAt must be UTC.", nameof(createdAt));

        Id = id;
        ArtistId = artistId;
        BodyPlacement = bodyPlacement;
        YearCompleted = yearCompleted;
        CreatedAt = createdAt;
        Title = title?.Trim();
        Description = description?.Trim();
        ApproximateSizeCm = approximateSizeCm;
        EstimatedSessionLengthHours = estimatedSessionLengthHours;
        ProgressionStatus = progressionStatus;
        LinkedBookingId = linkedBookingId;
    }
}
