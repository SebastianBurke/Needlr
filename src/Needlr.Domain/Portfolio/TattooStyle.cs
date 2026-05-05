namespace Needlr.Domain.Portfolio;

/// <summary>
/// A canonical or freeform tattoo style. Canonical styles are seeded by Needlr; freeform tags
/// can be promoted to canonical by an admin (FEATURE_SPECS.md § Tagging).
/// </summary>
public sealed class TattooStyle
{
    public const int NameMaxLength = 100;
    public const int SlugMaxLength = 100;

    public Guid Id { get; init; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public bool IsCanonical { get; set; }

    private TattooStyle() { }

    public TattooStyle(Guid id, string name, string slug, bool isCanonical)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > NameMaxLength)
            throw new ArgumentException($"Name must be <= {NameMaxLength} chars.", nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        if (slug.Length > SlugMaxLength)
            throw new ArgumentException($"Slug must be <= {SlugMaxLength} chars.", nameof(slug));

        Id = id;
        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        IsCanonical = isCanonical;
    }
}
