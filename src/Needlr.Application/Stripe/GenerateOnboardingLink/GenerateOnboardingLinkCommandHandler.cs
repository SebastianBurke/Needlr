using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Stripe.GenerateOnboardingLink;

internal sealed class GenerateOnboardingLinkCommandHandler(
    IStudioAuthorization studioAuthorization,
    IArtistRepository artists,
    IStripeService stripe) : IRequestHandler<GenerateOnboardingLinkCommand, Result<string>>
{
    public async Task<Result<string>> Handle(GenerateOnboardingLinkCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<string>.Failure(Error.Forbidden("Only artists can request an onboarding link."));

        var artist = await artists.GetByIdAsync(artistId.Value, cancellationToken);
        if (artist is null)
            return Result<string>.Failure(Error.NotFound("Artist"));

        if (string.IsNullOrEmpty(artist.StripeConnectAccountId))
            return Result<string>.Failure(Error.FailedPrecondition(
                "Create the Connect account first."));

        var url = await stripe.CreateAccountLinkAsync(
            artist.StripeConnectAccountId,
            request.ReturnUrl ?? string.Empty,
            request.RefreshUrl ?? string.Empty,
            cancellationToken);
        return Result<string>.Success(url);
    }
}
