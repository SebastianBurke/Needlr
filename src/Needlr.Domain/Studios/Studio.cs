using NetTopologySuite.Geometries;
using Needlr.Domain.Enums;
using Needlr.Domain.Verification;

namespace Needlr.Domain.Studios;

/// <summary>
/// A studio is a place where bookings happen — either a brick-and-mortar shop, a single-artist
/// solo studio, or a private invite-only collective. Per ADR-004, studios are managed by artists
/// (no separate StudioOwner role); admin rights are a per-affiliation permission.
/// </summary>
public sealed class Studio
{
    public const int NameMaxLength = 200;
    public const int AddressMaxLength = 500;
    public const int DescriptionMaxLength = 4000;

    public Guid Id { get; init; }
    public string Name { get; set; } = null!;
    public StudioType StudioType { get; set; }
    public Point Location { get; set; } = null!;
    public string Address { get; set; } = null!;
    public JoinPolicy JoinPolicy { get; set; }
    public string? Description { get; set; }
    public Guid CreatedByArtistId { get; init; }

    public ICollection<StudioHours> Hours { get; set; } = new List<StudioHours>();
    public ICollection<ArtistStudioAffiliation> Affiliations { get; set; } = new List<ArtistStudioAffiliation>();
    public ICollection<StudioCredential> Credentials { get; set; } = new List<StudioCredential>();

    private Studio() { }

    public Studio(
        Guid id,
        string name,
        StudioType studioType,
        Point location,
        string address,
        Guid createdByArtistId,
        JoinPolicy? joinPolicy = null,
        string? description = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > NameMaxLength)
            throw new ArgumentException($"Name must be <= {NameMaxLength} chars.", nameof(name));
        ArgumentNullException.ThrowIfNull(location);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);
        if (address.Length > AddressMaxLength)
            throw new ArgumentException($"Address must be <= {AddressMaxLength} chars.", nameof(address));
        if (createdByArtistId == Guid.Empty)
            throw new ArgumentException("CreatedByArtistId is required.", nameof(createdByArtistId));
        if (description is { Length: > DescriptionMaxLength })
            throw new ArgumentException($"Description must be <= {DescriptionMaxLength} chars.", nameof(description));

        Id = id;
        Name = name.Trim();
        StudioType = studioType;
        Location = location;
        Address = address.Trim();
        CreatedByArtistId = createdByArtistId;
        Description = description?.Trim();
        JoinPolicy = joinPolicy ?? DefaultJoinPolicyFor(studioType);
    }

    /// <summary>
    /// Per FEATURE_SPECS.md § Studio types: Solo defaults to Closed (single-artist by definition);
    /// Private and Shop default to InviteOnly (admin retains roster authority).
    /// </summary>
    public static JoinPolicy DefaultJoinPolicyFor(StudioType studioType) => studioType switch
    {
        StudioType.Solo => JoinPolicy.Closed,
        StudioType.Private => JoinPolicy.InviteOnly,
        StudioType.Shop => JoinPolicy.InviteOnly,
        _ => JoinPolicy.InviteOnly
    };
}
