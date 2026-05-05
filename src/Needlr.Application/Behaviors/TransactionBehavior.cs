using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;

namespace Needlr.Application.Behaviors;

/// <summary>
/// Auto-commits tracked changes around handlers that implement <see cref="ICommandBase"/>.
/// On a failed <see cref="Result"/>/<see cref="Result{T}"/>, changes are NOT persisted — the
/// request-scoped <c>NeedlrDbContext</c> is disposed and any tracked changes are discarded.
/// Queries (<see cref="IQuery{T}"/>) skip persistence entirely.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICommandBase)
            return await next();

        var response = await next();

        // Persist on success. For non-Result responses (rare), persist unconditionally.
        var shouldPersist = response is not Result result || result.IsSuccess;
        if (shouldPersist)
            await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }
}
