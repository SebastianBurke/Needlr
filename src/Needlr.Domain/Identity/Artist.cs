using Needlr.Domain.Enums;
using Needlr.Domain.Portfolio;
using Needlr.Domain.Studios;

namespace Needlr.Domain.Identity;

/// <summary>
/// Artist profile, one-to-one with the Identity user when <see cref="UserRole.Artist"/>. The artist's
/// physical location is derived from their primary <see cref="ArtistStudioAffiliation"/>; solo artists
/// have a Solo-type studio whose location is their working location (DOMAIN_MODEL.md § Artist).
/// </summary>
public sealed class Artist
{
    public const int DisplayNameMaxLength = 100;
    public const int BioMaxLength = 2000;
    public const int MaxStyleSelections = 6;

    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string DisplayName { get; set; } = null!;
    public string Bio { get; set; } = string.Empty;
    public int YearsExperience { get; set; }
    public decimal? HourlyRateCad { get; set; }
    public decimal? ShopMinimumCad { get; set; }
    public bool AcceptingNewBookings { get; set; } = true;
    public ArtistPaymentStatus PaymentStatus { get; set; } = ArtistPaymentStatus.NotOnboarded;
    public string? StripeConnectAccountId { get; set; }
    public CancellationPolicy CancellationPolicy { get; set; } = CancellationPolicy.Standard;

    /// <summary>
    /// Opaque token used to gate the per-artist iCal feed URL (FEATURE_SPECS.md § iCal export).
    /// Null until the artist requests their feed, at which point it's generated and persisted.
    /// Rotating the token invalidates any subscribed calendar clients.
    /// </summary>
    public string? IcalToken { get; set; }

    public ICollection<ArtistStudioAffiliation> Affiliations { get; set; } = new List<ArtistStudioAffiliation>();
    public ICollection<TattooStyle> Styles { get; set; } = new List<TattooStyle>();
    public ICollection<ArtistLeadTime> LeadTimes { get; set; } = new List<ArtistLeadTime>();

    private Artist() { }

    public Artist(
        Guid id,
        Guid userId,
        string displayName,
        string bio,
        int yearsExperience,
        decimal? hourlyRateCad = null,
        decimal? shopMinimumCad = null,
        CancellationPolicy cancellationPolicy = CancellationPolicy.Standard)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (displayName.Length > DisplayNameMaxLength)
            throw new ArgumentException($"DisplayName must be <= {DisplayNameMaxLength} chars.", nameof(displayName));
        bio ??= string.Empty;
        if (bio.Length > BioMaxLength)
            throw new ArgumentException($"Bio must be <= {BioMaxLength} chars.", nameof(bio));
        if (yearsExperience < 0)
            throw new ArgumentOutOfRangeException(nameof(yearsExperience), "Must be non-negative.");
        if (hourlyRateCad is < 0)
            throw new ArgumentOutOfRangeException(nameof(hourlyRateCad), "Must be non-negative.");
        if (shopMinimumCad is < 0)
            throw new ArgumentOutOfRangeException(nameof(shopMinimumCad), "Must be non-negative.");

        Id = id;
        UserId = userId;
        DisplayName = displayName.Trim();
        Bio = bio;
        YearsExperience = yearsExperience;
        HourlyRateCad = hourlyRateCad;
        ShopMinimumCad = shopMinimumCad;
        CancellationPolicy = cancellationPolicy;
    }
}
