namespace Needlr.Domain.Enums;

/// <summary>
/// Type of credential held at the studio level. Required types per jurisdiction are
/// configured on the Jurisdiction entity.
/// </summary>
public enum StudioCredentialType
{
    /// <summary>Annual public-health inspection. The issuing authority is jurisdiction-specific (Montréal: RSSS).</summary>
    HealthInspection,

    /// <summary>One-time municipal business registration document.</summary>
    MunicipalRegistration,

    Other
}
