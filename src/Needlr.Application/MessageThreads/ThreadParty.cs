using Needlr.Application.Abstractions.Persistence;
using Needlr.Domain.Bookings;

namespace Needlr.Application.MessageThreads;

/// <summary>
/// Resolves whether <paramref name="userId"/> is the customer or the artist on
/// <paramref name="booking"/>. Centralized so the various message handlers don't all
/// duplicate the artist's UserId lookup. Returns null when the user is neither party.
/// </summary>
internal static class ThreadParty
{
    public enum Role { Customer, Artist }

    public static async Task<Role?> ResolveAsync(
        Guid userId,
        Booking booking,
        IArtistRepository artists,
        CancellationToken cancellationToken)
    {
        if (booking.CustomerId == userId)
            return Role.Customer;
        var artist = await artists.GetByIdAsync(booking.ArtistId, cancellationToken);
        if (artist?.UserId == userId)
            return Role.Artist;
        return null;
    }
}
