using FluentAssertions;
using Needlr.Domain.Enums;
using Needlr.Domain.Verification;
using Xunit;

namespace Needlr.Domain.Tests.Verification;

public class ArtistCredentialTests
{
    private static readonly Guid Id = Guid.NewGuid();
    private static readonly Guid ArtistId = Guid.NewGuid();
    private static readonly Guid JurisdictionId = Guid.NewGuid();
    private static readonly DateOnly Issued = new(2026, 1, 1);
    private static readonly DateOnly Expiry = new(2027, 1, 1);

    [Fact]
    public void Ctor_ValidArgs_DefaultsToUnverified()
    {
        var cred = new ArtistCredential(Id, ArtistId, JurisdictionId, ArtistCredentialType.BloodbornePathogenCertification, Issued, Expiry);

        cred.VerificationStatus.Should().Be(VerificationStatus.Unverified);
        cred.CredentialType.Should().Be(ArtistCredentialType.BloodbornePathogenCertification);
    }

    [Fact]
    public void Ctor_EmptyId_Throws()
    {
        var act = () => new ArtistCredential(Guid.Empty, ArtistId, JurisdictionId, ArtistCredentialType.BloodbornePathogenCertification, Issued, Expiry);
        act.Should().Throw<ArgumentException>().WithParameterName("id");
    }

    [Fact]
    public void Ctor_EmptyArtistId_Throws()
    {
        var act = () => new ArtistCredential(Id, Guid.Empty, JurisdictionId, ArtistCredentialType.BloodbornePathogenCertification, Issued, Expiry);
        act.Should().Throw<ArgumentException>().WithParameterName("artistId");
    }

    [Fact]
    public void Ctor_EmptyJurisdictionId_Throws()
    {
        var act = () => new ArtistCredential(Id, ArtistId, Guid.Empty, ArtistCredentialType.BloodbornePathogenCertification, Issued, Expiry);
        act.Should().Throw<ArgumentException>().WithParameterName("jurisdictionId");
    }

    [Fact]
    public void Ctor_ExpiryNotAfterIssued_Throws()
    {
        var act = () => new ArtistCredential(Id, ArtistId, JurisdictionId, ArtistCredentialType.BloodbornePathogenCertification, Issued, Issued);
        act.Should().Throw<ArgumentException>().WithParameterName("expiryDate");
    }
}
