using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Bookings;
using Needlr.Domain.Enums;

namespace Needlr.Application.Bookings.RequestBooking;

internal sealed class RequestBookingCommandHandler(
    ICurrentUser currentUser,
    IArtistRepository artists,
    IArtistStudioAffiliationRepository affiliations,
    IArtistLeadTimeRepository leadTimes,
    IBookingRepository bookings,
    IContactInfoStripper stripper,
    IStripeService stripe,
    IBookingExpiryScheduler expiryScheduler,
    INotificationDispatcher notifications,
    IModerationService moderation,
    IClock clock) : IRequestHandler<RequestBookingCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RequestBookingCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(UserRole.Customer))
            return Result<Guid>.Failure(Error.Forbidden("Only customers can submit booking requests."));

        var customerId = currentUser.UserId
            ?? throw new InvalidOperationException("Authenticated customer must have a UserId claim.");

        // Suspended customer can't make new bookings (existing ones are honored elsewhere).
        if (await moderation.IsSuspendedAsync(customerId, cancellationToken))
            return Result<Guid>.Failure(Error.Forbidden("Account is suspended."));

        var artist = await artists.GetByIdAsync(request.ArtistId, cancellationToken);
        if (artist is null)
            return Result<Guid>.Failure(Error.NotFound("Artist"));
        if (!artist.AcceptingNewBookings)
            return Result<Guid>.Failure(Error.FailedPrecondition("Artist is not accepting new bookings."));

        // Suspended artist can't accept new bookings — equivalent to NotFound from the
        // customer's perspective so the suspension state isn't probable.
        if (await moderation.IsSuspendedAsync(artist.UserId, cancellationToken))
            return Result<Guid>.Failure(Error.NotFound("Artist"));

        // Per ADR-005 deposits flow to the artist's connected account. Until Stripe says
        // the artist's account is fully onboarded we don't accept bookings — taking a
        // pre-auth on a Restricted/OnboardingInProgress account would silently fail later.
        if (artist.PaymentStatus != ArtistPaymentStatus.Active
            || string.IsNullOrEmpty(artist.StripeConnectAccountId))
            return Result<Guid>.Failure(Error.FailedPrecondition(
                "Artist has not completed payment onboarding."));

        // Lead-time check. Per FEATURE_SPECS § Lead time, the request is rejected if the
        // requested date is before today + minimum-days. If the artist hasn't set a row for
        // this booking type, fall back to the platform defaults.
        var artistLeadTimes = await leadTimes.ListByArtistAsync(artist.Id, cancellationToken);
        var minDays = artistLeadTimes
            .Where(lt => lt.BookingType == request.BookingType)
            .Select(lt => (int?)lt.MinimumDays)
            .FirstOrDefault() ?? DefaultLeadDaysFor(request.BookingType);

        var today = DateOnly.FromDateTime(clock.UtcNow);
        if (request.RequestedDate < today.AddDays(minDays))
            return Result<Guid>.Failure(Error.FailedPrecondition(
                $"Requested date must be at least {minDays} day(s) from today for {request.BookingType}."));

        // Pin the booking to the artist's primary active studio. If none is marked primary,
        // pick the most-recently-started Active affiliation. If none at all, the booking
        // can't proceed — we need a venue for the session.
        var artistAffiliations = await affiliations.ListByArtistAsync(artist.Id, cancellationToken);
        var venue = artistAffiliations
            .Where(a => a.Status == AffiliationStatus.Active)
            .OrderByDescending(a => a.IsPrimary)
            .ThenByDescending(a => a.StartDate)
            .FirstOrDefault();
        if (venue is null)
            return Result<Guid>.Failure(Error.FailedPrecondition(
                "Artist has no active studio affiliation; cannot host a booking."));

        var deposit = BookingDefaults.DefaultDepositCad;

        // Pre-authorize the deposit on the artist's connected account. capture_method=manual
        // so funds aren't taken yet; capture lands on artist accept (Phase 11 webhook flow).
        var intent = await stripe.CreatePaymentIntentAsync(
            deposit,
            request.CustomerPaymentMethodId,
            artist.StripeConnectAccountId!,
            cancellationToken);

        var requestedAt = clock.UtcNow;
        var booking = new Booking(
            id: Guid.NewGuid(),
            customerId: customerId,
            artistId: artist.Id,
            studioId: venue.StudioId,
            bookingType: request.BookingType,
            requestedAt: requestedAt,
            requestedDate: request.RequestedDate,
            estimatedDurationHours: request.EstimatedDurationHours,
            description: stripper.Strip(request.Description),
            bodyPlacement: request.BodyPlacement,
            depositAmountCad: deposit,
            cancellationPolicySnapshot: artist.CancellationPolicy,
            approximateSizeCm: request.ApproximateSizeCm,
            estimatedTotalCad: request.EstimatedTotalCad)
        {
            StripePaymentIntentId = intent.PaymentIntentId
        };
        bookings.Add(booking);

        // 7-day auto-expire (Hangfire). Idempotent expire handler tolerates earlier
        // accept/decline beating the timer.
        expiryScheduler.Schedule(booking.Id, requestedAt.AddDays(7));

        // Notify the artist that a new request has landed (FEATURE_SPECS § Notifications).
        await notifications.DispatchAsync(
            artist.UserId,
            NotificationType.NewBookingRequest,
            new NotificationContent(
                EmailSubject: "New booking request on Needlr",
                EmailBody: $"You have a new booking request from a customer for {request.RequestedDate:yyyy-MM-dd}. Open Needlr to review.",
                PushTitle: "New booking request",
                PushBody: $"Request for {request.RequestedDate:yyyy-MM-dd}"),
            cancellationToken);

        return Result<Guid>.Success(booking.Id);
    }

    /// <summary>
    /// Platform defaults from FEATURE_SPECS.md § Artist onboarding step 11: Consultation 3,
    /// TattooSession 7, Touchup 7. Used when an artist hasn't customized their lead times.
    /// </summary>
    private static int DefaultLeadDaysFor(BookingType type) => type switch
    {
        BookingType.Consultation => 3,
        BookingType.TattooSession => 7,
        BookingType.Touchup => 7,
        _ => 7
    };
}
