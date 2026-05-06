namespace Needlr.Domain.Enums;

/// <summary>
/// Type of credential held by an individual artist. Whether a practice license applies is
/// jurisdiction-driven (<see cref="Verification.Jurisdiction.RequiresArtistLicense"/>); the
/// values here cover both license-required and training/hygiene-only jurisdictions.
/// </summary>
public enum ArtistCredentialType
{
    /// <summary>Annual bloodborne-pathogen certification (training certificate).</summary>
    BloodbornePathogenCertification,

    /// <summary>Hygiene/sanitation training. Named for Quebec's "formation hygiène et salubrité"; equivalent training programs in other jurisdictions map to this credential type.</summary>
    FormationHygieneEtSalubrite,

    /// <summary>Practitioner license, where the jurisdiction requires one. Not used in Quebec.</summary>
    LicensePractitioner,

    Other
}
