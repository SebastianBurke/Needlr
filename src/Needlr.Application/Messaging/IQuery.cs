using MediatR;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Messaging;

/// <summary>
/// Read-only request. Returns <typeparamref name="TResponse"/> wrapped in a <see cref="Result{T}"/>
/// to make NotFound and authorization-style failures explicit at the call site.
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
