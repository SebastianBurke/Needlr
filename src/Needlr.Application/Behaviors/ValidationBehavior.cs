using FluentValidation;
using MediatR;

namespace Needlr.Application.Behaviors;

/// <summary>
/// Runs all registered <see cref="IValidator{T}"/>s for the incoming request and throws
/// <see cref="ValidationException"/> on failures. The Api layer's exception middleware
/// translates that into a 400 response with the per-property errors.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToArray();

        if (failures.Length != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
