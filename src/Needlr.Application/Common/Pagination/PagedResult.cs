namespace Needlr.Application.Common.Pagination;

/// <summary>
/// One page of results plus the metadata needed to render pagination controls.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Empty(PageRequest request) =>
        new([], request.Page, request.PageSize, 0);
}
