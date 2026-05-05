using NetTopologySuite.Geometries;

namespace Needlr.Application.Common.Geography;

/// <summary>
/// Conversions between <see cref="GeoPoint"/> (Application-level lat/lng record) and
/// <see cref="Point"/> (NetTopologySuite spatial type used by Domain entities and EF/PostGIS).
/// </summary>
public static class GeographyExtensions
{
    public const int Wgs84Srid = 4326;

    /// <summary>WGS84 lon/lat order — NetTopologySuite uses (X, Y) where X is longitude.</summary>
    public static Point ToPoint(this GeoPoint geo) =>
        new(geo.Longitude, geo.Latitude) { SRID = Wgs84Srid };

    public static GeoPoint ToGeoPoint(this Point point) =>
        new(Latitude: point.Y, Longitude: point.X);
}
