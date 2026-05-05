using FluentAssertions;
using Needlr.Domain.Enums;
using Needlr.Domain.Verification;
using Xunit;

namespace Needlr.Domain.Tests.Verification;

public class StudioCredentialTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid StudioId = Guid.NewGuid();
    private static readonly Guid JurisdictionId = Guid.NewGuid();
    private static readonly DateOnly Issued = new(2026, 1, 1);
    private static readonly DateOnly Expiry = new(2027, 1, 1);

    [Fact]
    public void Ctor_ValidArgs_DefaultsToUnverified()
    {
        var cred = new StudioCredential(Id, StudioId, JurisdictionId, StudioCredentialType.HealthInspection, Issued, Expiry, "https://example/doc.pdf");

        cred.VerificationStatus.Should().Be(VerificationStatus.Unverified);
        cred.DocumentUrl.Should().Be("https://example/doc.pdf");
    }

    [Fact]
    public void Ctor_EmptyId_Throws()
    {
        var act = () => new StudioCredential(Guid.Empty, StudioId, JurisdictionId, StudioCredentialType.HealthInspection, Issued, Expiry);
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Fact]
    public void Ctor_EmptyStudioId_Throws()
    {
        var act = () => new StudioCredential(Id, Guid.Empty, JurisdictionId, StudioCredentialType.HealthInspection, Issued, Expiry);
        act.Should().Throw<ArgumentException>().WithParameterName("studioId");
    }

    [Fact]
    public void Ctor_EmptyJurisdictionId_Throws()
    {
        var act = () => new StudioCredential(Id, StudioId, Guid.Empty, StudioCredentialType.HealthInspection, Issued, Expiry);
        act.Should().Throw<ArgumentException>().WithParameterName("jurisdictionId");
    }

    [Fact]
    public void Ctor_ExpiryNotAfterIssued_Throws()
    {
        var act = () => new StudioCredential(Id, StudioId, JurisdictionId, StudioCredentialType.HealthInspection, Issued, Issued);
        act.Should().Throw<ArgumentException>().WithParameterName("expiryDate");
    }
}
