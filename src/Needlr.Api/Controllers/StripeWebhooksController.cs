using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Application.Abstractions;

namespace Needlr.Api.Controllers;

[ApiController]
[Route("api/webhooks/stripe")]
[AllowAnonymous]
public sealed class StripeWebhooksController(IStripeWebhookProcessor processor) : ControllerBase
{
    private const string SignatureHeader = "Stripe-Signature";
    private readonly IStripeWebhookProcessor _processor = processor;

    /// <summary>
    /// Inbound Stripe Connect webhook. Reads the raw request body (signature verification
    /// requires byte-exact bytes — Stripe.net's <c>EventUtility.ConstructEvent</c> hashes
    /// the payload). Idempotency + dispatch live in the processor.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();

        using var reader = new StreamReader(
            Request.Body, encoding: System.Text.Encoding.UTF8, leaveOpen: true);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        var signature = Request.Headers[SignatureHeader].ToString();
        var outcome = await _processor.ProcessAsync(payload, signature, cancellationToken);

        return outcome switch
        {
            StripeWebhookOutcome.Processed => Ok(),
            StripeWebhookOutcome.InvalidSignature => BadRequest(),
            StripeWebhookOutcome.Error => StatusCode(StatusCodes.Status500InternalServerError),
            _ => StatusCode(StatusCodes.Status500InternalServerError),
        };
    }
}
