using Needlr.Application.Abstractions;

namespace Needlr.Infrastructure.Common;

internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
