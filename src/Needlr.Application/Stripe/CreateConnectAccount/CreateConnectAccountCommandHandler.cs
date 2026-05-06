using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;
using Needlr.Domain.Enums;

namespace Needlr.Application.Stripe.CreateConnectAccount;

internal sealed class CreateConnectAccountCommandHandler(
    IStudioAuthorization studioAuthorization,
    ICurrentUser currentUser,
    IArtistRepository artists,
    IStripeService stripe) : IRequestHandler<CreateConnectAccountCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CreateConnectAccountCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<string>.Failure(Error.Forbidden("Only artists can create a Connect account."));

        var email = currentUser.Email;
        if (string.IsNullOrWhiteSpace(email))
            return Result<string>.Failure(Error.FailedPrecondition("User has no email on file."));

        var artist = await artists.GetByIdAsync(artistId.Value, cancellationToken);
        if (artist is null)
            return Result<string>.Failure(Error.NotFound("Artist"));

        if (!string.IsNullOrEmpty(artist.StripeConnectAccountId))
            return Result<string>.Success(artist.StripeConnectAccountId);

        var created = await stripe.CreateConnectAccountAsync(email, cancellationToken);
        artist.StripeConnectAccountId = created.ConnectAccountId;
        if (artist.PaymentStatus == ArtistPaymentStatus.NotOnboarded)
            artist.PaymentStatus = ArtistPaymentStatus.OnboardingInProgress;

        return Result<string>.Success(created.ConnectAccountId);
    }
}
