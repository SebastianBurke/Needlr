namespace Needlr.Web.Services;

/// <summary>
/// Friendly labels for domain enum values surfaced in the UI. The API returns enum
/// names verbatim ("TattooSession", "DocumentsSubmitted", "FullSleeve"); this maps
/// them to phrasing a customer or artist would actually read on screen. Unknown
/// values fall through to the original string so we never paint an empty cell.
/// </summary>
internal static class EnumLabels
{
    public static string BookingType(string? raw) => raw switch
    {
        "TattooSession" => "Tattoo session",
        "Consultation"  => "Consultation",
        "Touchup"       => "Touch-up",
        _               => raw ?? string.Empty
    };

    public static string Verification(string? raw) => raw switch
    {
        "Verified"           => "Verified",
        "DocumentsSubmitted" => "Documents pending",
        "Unverified"         => "Unverified",
        "Rejected"           => "Rejected",
        "Expired"            => "Expired",
        _                    => raw ?? string.Empty
    };

    public static string BodyPlacement(string? raw) => raw switch
    {
        null or "" => string.Empty,
        "UpperArm"  => "Upper arm",
        "FullSleeve"=> "Full sleeve",
        "HalfSleeve"=> "Half sleeve",
        "UpperBack" => "Upper back",
        "LowerBack" => "Lower back",
        "FullBack"  => "Full back",
        _           => raw
    };
}
