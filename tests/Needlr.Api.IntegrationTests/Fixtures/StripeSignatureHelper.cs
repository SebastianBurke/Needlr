using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Needlr.Api.IntegrationTests.Fixtures;

/// <summary>
/// Generates a Stripe-Signature header that <c>EventUtility.ConstructEvent</c> will accept,
/// using the same HMAC-SHA256 scheme Stripe documents at
/// https://stripe.com/docs/webhooks/signatures. The constructed string is
/// <c>t={timestamp},v1={signature}</c>.
/// </summary>
internal static class StripeSignatureHelper
{
    public static string Sign(string payload, string secret, DateTimeOffset? at = null)
    {
        var ts = (at ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        var signedPayload = $"{ts}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        return string.Format(CultureInfo.InvariantCulture, "t={0},v1={1}", ts, hex);
    }
}
