namespace Needlr.Application.Abstractions;

/// <summary>
/// Abstraction over <see cref="DateTime.UtcNow"/> so handlers and behaviors can be tested with
/// a fixed, advancing, or scripted clock.
/// </summary>
public interface IClock
{
    /// <summary>Current UTC time. Always <see cref="DateTimeKind.Utc"/>.</summary>
    DateTime UtcNow { get; }
}
