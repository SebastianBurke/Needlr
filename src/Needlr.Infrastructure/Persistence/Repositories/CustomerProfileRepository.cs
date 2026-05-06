using Microsoft.EntityFrameworkCore;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Identity;

namespace Needlr.Infrastructure.Persistence.Repositories;

internal sealed class CustomerProfileRepository(NeedlrDbContext db) : ICustomerProfileRepository
{
    private readonly NeedlrDbContext _db = db;

    public Task<CustomerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _db.CustomerProfiles.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
}
