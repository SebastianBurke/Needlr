using MediatR;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Messaging;

/// <summary>
/// State-changing request that returns no value on success.
/// </summary>
public interface ICommand : ICommandBase, IRequest<Result>;

/// <summary>
/// State-changing request that returns <typeparamref name="TResponse"/> on success.
/// </summary>
public interface ICommand<TResponse> : ICommandBase, IRequest<Result<TResponse>>;
