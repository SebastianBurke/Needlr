namespace Needlr.Application.Abstractions;

/// <summary>
/// Pre-acceptance content guard. Strips phone numbers, email addresses, and common social
/// handles out of free-text fields on inbound booking requests so artists and customers
/// can't backchannel before a booking is accepted (FEATURE_SPECS.md § Pre-acceptance content
/// stripping). Post-acceptance threads do their own thing — this only runs at the request /
/// respond-with-info entry points.
/// </summary>
public interface IContactInfoStripper
{
    /// <summary>
    /// Returns the input with phone numbers, email addresses, URLs, and @-handles replaced
    /// by a placeholder. Output is trimmed; whitespace runs are collapsed to a single space.
    /// </summary>
    string Strip(string input);
}
