using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Needlr.Application.Behaviors;

namespace Needlr.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR (assembly scan), FluentValidation validators (assembly scan), and the
    /// pipeline behaviors. Behavior order — Logging (outermost) → Validation → Transaction
    /// (innermost) — is intentional: logging captures everything; validation rejects bad
    /// requests before a transaction starts.
    /// </summary>
    public static IServiceCollection AddNeedlrApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        return services;
    }
}
