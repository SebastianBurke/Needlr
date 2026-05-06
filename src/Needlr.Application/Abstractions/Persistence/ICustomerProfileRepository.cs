using Needlr.Domain.Identity;

namespace Needlr.Application.Abstractions.Persistence;

public interface ICustomerProfileRepository
{
    /// <summary>Resolves the customer profile tied to an authenticated user.</summary>
    Task<CustomerProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
