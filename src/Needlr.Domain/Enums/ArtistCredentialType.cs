namespace Needlr.Domain.Enums;

/// <summary>
/// Type of credential held by an individual artist. Note Quebec does not license individual
/// tattoo artists; these credentials concern training/hygiene rather than a practice license.
/// </summary>
public enum ArtistCredentialType
{
    /// <summary>Annual bloodborne-pathogen certification (training certificate).</summary>
    BloodbornePathogenCertification,

    /// <summary>Quebec-specific hygiene and sanitation training (formation hygiène et salubrité).</summary>
    FormationHygieneEtSalubrite,

    /// <summary>Practitioner license, where the jurisdiction requires one (not used in Quebec).</summary>
    LicensePractitioner,

    Other
}
