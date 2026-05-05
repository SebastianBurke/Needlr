using Needlr.Domain.Enums;

namespace Needlr.Application.Abstractions;

/// <summary>
/// High-level user-account operations sitting on top of ASP.NET Core Identity's
/// <c>UserManager&lt;ApplicationUser&gt;</c>. Defined in Application so handlers don't depend
/// on Identity types directly; implementation in Infrastructure performs the registration
/// (user creation + role-specific profile creation) atomically inside a single DB transaction.
/// </summary>
public interface IUserAccountService
{
    /// <summary>Creates an <c>ApplicationUser</c> + <c>CustomerProfile</c> in one transaction.</summary>
    Task<UserRegistrationResult> RegisterCustomerAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default);

    /// <summary>Creates an <c>ApplicationUser</c> + <c>Artist</c> in one transaction.</summary>
    Task<UserRegistrationResult> RegisterArtistAsync(
        string email,
        string password,
        string displayName,
        int yearsExperience,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the email/password pair. Returns null on unknown email or wrong password —
    /// callers must not distinguish the two failure modes in their response (per OWASP).
    /// </summary>
    Task<UserInfo?> CheckCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<UserInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>Outcome of a <see cref="IUserAccountService.RegisterCustomerAsync"/> /
/// <see cref="IUserAccountService.RegisterArtistAsync"/> call.</summary>
public sealed record UserRegistrationResult(
    bool Succeeded,
    Guid UserId,
    IReadOnlyList<string> Errors)
{
    public static UserRegistrationResult Success(Guid userId) => new(true, userId, []);

    public static UserRegistrationResult Failure(params string[] errors) =>
        new(false, Guid.Empty, errors);

    public static UserRegistrationResult Failure(IEnumerable<string> errors) =>
        new(false, Guid.Empty, errors.ToArray());
}

public sealed record UserInfo(Guid UserId, string Email, UserRole Role);
