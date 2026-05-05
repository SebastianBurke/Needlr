namespace Needlr.Application.Common.Geography;

/// <summary>
/// A latitude/longitude pair in WGS84 (degrees). Application-layer DTO; converted to
/// <c>NetTopologySuite.Geometries.Point</c> at the persistence boundary.
/// </summary>
public sealed record GeoPoint(double Latitude, double Longitude)
{
    public const double MinLatitude = -90.0;
    public const double MaxLatitude = 90.0;
    public const double MinLongitude = -180.0;
    public const double MaxLongitude = 180.0;

    public bool IsValid =>
        Latitude is >= MinLatitude and <= MaxLatitude &&
        Longitude is >= MinLongitude and <= MaxLongitude;
}
