using Needlr.Domain.Moderation;

namespace Needlr.Application.Abstractions.Persistence;

public interface IUserWarningRepository
{
    Task<IReadOnlyList<UserWarning>> ListByUserAsync(
        Guid userId, CancellationToken cancellationToken = default);

    void Add(UserWarning warning);
}
