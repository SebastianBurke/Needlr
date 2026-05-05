using NetTopologySuite.Geometries;
using Needlr.Domain.Portfolio;

namespace Needlr.Domain.Identity;

/// <summary>
/// Customer-side profile, one-to-one with the Identity user when <see cref="Enums.UserRole.Customer"/>.
/// Optional location is used as the default map center.
/// </summary>
public sealed class CustomerProfile
{
    public const int DisplayNameMaxLength = 100;
    public const int MinSearchRadiusKm = 1;
    public const int MaxSearchRadiusKm = 200;
    public const int DefaultSearchRadiusKm = 15;

    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string DisplayName { get; set; } = null!;
    public Point? Location { get; set; }
    public int PreferredSearchRadiusKm { get; set; } = DefaultSearchRadiusKm;
    public ICollection<TattooStyle> PreferredStyles { get; set; } = new List<TattooStyle>();

    private CustomerProfile() { }

    public CustomerProfile(
        Guid id,
        Guid userId,
        string displayName,
        int preferredSearchRadiusKm = DefaultSearchRadiusKm,
        Point? location = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (userId == Guid.Empty) throw new ArgumentException("UserId is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (displayName.Length > DisplayNameMaxLength)
            throw new ArgumentException($"DisplayName must be <= {DisplayNameMaxLength} chars.", nameof(displayName));
        if (preferredSearchRadiusKm is < MinSearchRadiusKm or > MaxSearchRadiusKm)
            throw new ArgumentOutOfRangeException(nameof(preferredSearchRadiusKm),
                $"Must be in [{MinSearchRadiusKm}, {MaxSearchRadiusKm}].");

        Id = id;
        UserId = userId;
        DisplayName = displayName.Trim();
        PreferredSearchRadiusKm = preferredSearchRadiusKm;
        Location = location;
    }
}
