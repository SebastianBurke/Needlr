using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.MessageThreads.GetThreadByBooking;

internal sealed class GetThreadByBookingQueryHandler(
    ICurrentUser currentUser,
    IBookingRepository bookings,
    IMessageThreadRepository threads,
    IArtistRepository artists)
    : IRequestHandler<GetThreadByBookingQuery, Result<ThreadDto?>>
{
    public async Task<Result<ThreadDto?>> Handle(
        GetThreadByBookingQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
            return Result<ThreadDto?>.Failure(Error.Unauthorized());

        var booking = await bookings.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
            return Result<ThreadDto?>.Failure(Error.NotFound("Booking"));

        // Authorize: only the booking parties (or admins) can resolve the thread. Mirrors
        // the visibility rule used by GetThreadMessagesQueryHandler so a stranger can't
        // probe whether a thread exists for an arbitrary booking id.
        var role = await ThreadParty.ResolveAsync(
            currentUser.UserId.Value, booking, artists, cancellationToken);
        var isAdmin = currentUser.IsInRole(UserRole.Admin);
        if (role is null && !isAdmin)
            return Result<ThreadDto?>.Failure(Error.Forbidden("Not a party to this booking."));

        var thread = await threads.GetByBookingIdAsync(request.BookingId, cancellationToken);
        if (thread is null)
            return Result<ThreadDto?>.Success(null);

        return Result<ThreadDto?>.Success(new ThreadDto(
            thread.Id, thread.BookingId, thread.OpenedAt, thread.LockedAt, thread.Status, null));
    }
}
