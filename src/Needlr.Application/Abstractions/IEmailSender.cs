namespace Needlr.Application.Abstractions;

/// <summary>
/// Sends transactional email. Phase 13 will add per-channel notification preferences and
/// templated content; for now the contract is plain-text body. Implementations: console
/// logger for dev, SendGrid (or equivalent) for prod.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
