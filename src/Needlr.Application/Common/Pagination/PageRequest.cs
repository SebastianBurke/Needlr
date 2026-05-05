namespace Needlr.Application.Common.Pagination;

/// <summary>
/// Page coordinates for paginated queries. <see cref="Page"/> is 1-based.
/// </summary>
public sealed record PageRequest(int Page = 1, int PageSize = 20)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>Zero-based offset for SQL <c>OFFSET</c> / EF <c>Skip</c>.</summary>
    public int Skip => (Page - 1) * PageSize;

    /// <summary>Returns a normalized PageRequest clamped to safe values.</summary>
    public PageRequest Clamp() => new(
        Math.Max(1, Page),
        Math.Clamp(PageSize, 1, MaxPageSize));
}
