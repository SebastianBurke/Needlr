using Microsoft.AspNetCore.Identity;
using Needlr.Application.Abstractions;
using Needlr.Domain.Enums;
using Needlr.Domain.Identity;
using Needlr.Infrastructure.Persistence;

namespace Needlr.Infrastructure.Identity;

internal sealed class UserAccountService(
    UserManager<ApplicationUser> userManager,
    NeedlrDbContext db,
    IClock clock) : IUserAccountService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly NeedlrDbContext _db = db;
    private readonly IClock _clock = clock;

    public async Task<UserRegistrationResult> RegisterCustomerAsync(
        string email, string password, string displayName, CancellationToken cancellationToken = default)
    {
        // UserManager.CreateAsync calls SaveChangesAsync internally; wrap the user-create + the
        // profile insert in an explicit transaction so a profile-creation failure rolls back the
        // user too. EF Core's request-scoped DbContext shares its connection here.
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            CreatedAt = _clock.UtcNow,
            Role = UserRole.Customer
        };

        var identityResult = await _userManager.CreateAsync(user, password);
        if (!identityResult.Succeeded)
        {
            await tx.RollbackAsync(cancellationToken);
            return UserRegistrationResult.Failure(identityResult.Errors.Select(e => e.Description));
        }

        var profile = new CustomerProfile(Guid.NewGuid(), user.Id, displayName);
        _db.CustomerProfiles.Add(profile);
        await _db.SaveChangesAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return UserRegistrationResult.Success(user.Id);
    }

    public async Task<UserRegistrationResult> RegisterArtistAsync(
        string email, string password, string displayName, int yearsExperience, CancellationToken cancellationToken = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            CreatedAt = _clock.UtcNow,
            Role = UserRole.Artist
        };

        var identityResult = await _userManager.CreateAsync(user, password);
        if (!identityResult.Succeeded)
        {
            await tx.RollbackAsync(cancellationToken);
            return UserRegistrationResult.Failure(identityResult.Errors.Select(e => e.Description));
        }

        // Phase 4 produces a minimal Artist row — bio is empty until the artist completes the
        // multi-step onboarding (Phase 20). Bio is required-non-null per the entity, so we pass
        // empty string explicitly.
        var artist = new Artist(
            id: Guid.NewGuid(),
            userId: user.Id,
            displayName: displayName,
            bio: string.Empty,
            yearsExperience: yearsExperience);
        _db.Artists.Add(artist);
        await _db.SaveChangesAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);
        return UserRegistrationResult.Success(user.Id);
    }

    public async Task<UserInfo?> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return null;
        if (!await _userManager.CheckPasswordAsync(user, password)) return null;
        return new UserInfo(user.Id, user.Email!, user.Role);
    }

    public async Task<UserInfo?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : new UserInfo(user.Id, user.Email!, user.Role);
    }
}
