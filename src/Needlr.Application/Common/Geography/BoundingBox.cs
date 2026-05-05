namespace Needlr.Application.Common.Geography;

/// <summary>
/// Geographic bounding box used by the discovery map query. Latitudes are validated
/// (south &lt;= north); longitudes are not (boxes can legally wrap the antimeridian).
/// </summary>
public sealed record BoundingBox(double SouthLat, double WestLng, double NorthLat, double EastLng)
{
    public bool IsValid =>
        SouthLat is >= GeoPoint.MinLatitude and <= GeoPoint.MaxLatitude &&
        NorthLat is >= GeoPoint.MinLatitude and <= GeoPoint.MaxLatitude &&
        WestLng is >= GeoPoint.MinLongitude and <= GeoPoint.MaxLongitude &&
        EastLng is >= GeoPoint.MinLongitude and <= GeoPoint.MaxLongitude &&
        SouthLat <= NorthLat;

    /// <summary>True if the box crosses the antimeridian (180°/-180° line).</summary>
    public bool CrossesAntimeridian => WestLng > EastLng;
}
