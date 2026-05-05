namespace Needlr.Domain.Studios;

/// <summary>
/// Weekly hours-of-operation for a studio. One row per day-of-week per studio.
/// </summary>
public sealed class StudioHours
{
    public Guid Id { get; init; }
    public Guid StudioId { get; init; }
    public DayOfWeek DayOfWeek { get; init; }
    public TimeOnly OpenTime { get; set; }
    public TimeOnly CloseTime { get; set; }

    /// <summary>
    /// True means the studio is closed that day regardless of <see cref="OpenTime"/> / <see cref="CloseTime"/>.
    /// </summary>
    public bool IsClosed { get; set; }

    private StudioHours() { }

    public StudioHours(
        Guid id,
        Guid studioId,
        DayOfWeek dayOfWeek,
        TimeOnly openTime,
        TimeOnly closeTime,
        bool isClosed = false)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (studioId == Guid.Empty) throw new ArgumentException("StudioId is required.", nameof(studioId));
        if (!isClosed && closeTime <= openTime)
            throw new ArgumentException("CloseTime must be after OpenTime when the studio is open.");

        Id = id;
        StudioId = studioId;
        DayOfWeek = dayOfWeek;
        OpenTime = openTime;
        CloseTime = closeTime;
        IsClosed = isClosed;
    }
}
