namespace Needlr.Domain.Verification;

/// <summary>
/// Geographic / regulatory jurisdiction whose rules govern verification. Seeded with Montréal as
/// the only row at launch; the schema exists to accommodate v2+ expansion to other cities.
/// </summary>
public sealed class Jurisdiction
{
    public const int NameMaxLength = 200;
    public const int CountryMaxLength = 100;
    public const int RegionMaxLength = 100;
    public const int CityMaxLength = 100;

    public Guid Id { get; init; }
    public string Name { get; set; } = null!;
    public string Country { get; set; } = null!;
    public string Region { get; set; } = null!;
    public string City { get; set; } = null!;

    public bool RequiresStudioInspection { get; set; }
    public bool RequiresArtistLicense { get; set; }
    public bool RequiresArtistHygieneTraining { get; set; }
    public bool RequiresBloodbornePathogenCert { get; set; }

    private Jurisdiction() { }

    public Jurisdiction(
        Guid id,
        string name,
        string country,
        string region,
        string city,
        bool requiresStudioInspection,
        bool requiresArtistLicense,
        bool requiresArtistHygieneTraining,
        bool requiresBloodbornePathogenCert)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Length > NameMaxLength)
            throw new ArgumentException($"Name must be <= {NameMaxLength} chars.", nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        if (country.Length > CountryMaxLength)
            throw new ArgumentException($"Country must be <= {CountryMaxLength} chars.", nameof(country));
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        if (region.Length > RegionMaxLength)
            throw new ArgumentException($"Region must be <= {RegionMaxLength} chars.", nameof(region));
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        if (city.Length > CityMaxLength)
            throw new ArgumentException($"City must be <= {CityMaxLength} chars.", nameof(city));

        Id = id;
        Name = name.Trim();
        Country = country.Trim();
        Region = region.Trim();
        City = city.Trim();
        RequiresStudioInspection = requiresStudioInspection;
        RequiresArtistLicense = requiresArtistLicense;
        RequiresArtistHygieneTraining = requiresArtistHygieneTraining;
        RequiresBloodbornePathogenCert = requiresBloodbornePathogenCert;
    }
}
