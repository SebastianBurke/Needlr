namespace Needlr.Application.Abstractions;

/// <summary>
/// Persistence boundary used by handlers. Implementation is the EF Core
/// <c>NeedlrDbContext</c> in Infrastructure (registered as <c>IUnitOfWork</c> in DI).
/// The <see cref="Behaviors.TransactionBehavior{TRequest,TResponse}"/> calls
/// <see cref="SaveChangesAsync"/> automatically on a successful command result.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persists tracked changes. Returns the affected row count.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
