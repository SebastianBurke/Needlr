using System.Text.RegularExpressions;
using Needlr.Application.Abstractions;

namespace Needlr.Infrastructure.Common;

/// <summary>
/// Regex-based stripper. Conservative — it errs toward over-stripping rather than letting
/// contact info through, since the front-end shows the user the stripped result before
/// submit (per FEATURE_SPECS § Pre-acceptance content stripping) so false positives are
/// recoverable. The replacement token is intentionally human-readable so customers
/// understand what happened.
/// </summary>
internal sealed partial class ContactInfoStripper : IContactInfoStripper
{
    public const string Replacement = "[contact info hidden]";

    public string Strip(string input)
    {
        if (string.IsNullOrEmpty(input)) return input ?? string.Empty;

        var s = EmailRegex().Replace(input, Replacement);
        s = UrlRegex().Replace(s, Replacement);
        // Phone numbers must run after URL so we don't try to strip "://" patterns as digits.
        s = PhoneRegex().Replace(s, Replacement);
        s = HandleRegex().Replace(s, Replacement);

        // Collapse whitespace runs that may now bracket the placeholder.
        s = WhitespaceRegex().Replace(s, " ").Trim();
        return s;
    }

    // user@host.tld with at least one dot in the domain.
    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    // http(s):// or bare www. URLs.
    [GeneratedRegex(@"\b(?:https?://|www\.)[^\s,;]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    // Phone numbers — at least 7 digits, optionally separated by spaces, dashes, dots, or
    // parentheses, optional leading + and 1-3 digit country code. Avoids matching short
    // sequences like "8h" or piece dimensions.
    [GeneratedRegex(@"(?<!\w)(?:\+?\d{1,3}[\s.\-]?)?(?:\(\d{1,4}\)[\s.\-]?)?\d{2,4}[\s.\-]?\d{2,4}[\s.\-]?\d{2,4}(?!\w)")]
    private static partial Regex PhoneRegex();

    // @handle with at least 3 chars, common across IG/TikTok/X.
    [GeneratedRegex(@"(?<!\S)@[A-Za-z0-9._]{3,}", RegexOptions.IgnoreCase)]
    private static partial Regex HandleRegex();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex WhitespaceRegex();
}
