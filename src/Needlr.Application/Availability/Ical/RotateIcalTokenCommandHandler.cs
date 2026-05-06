using System.Security.Cryptography;
using MediatR;
using Needlr.Application.Abstractions;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Application.Common.Results;

namespace Needlr.Application.Availability.Ical;

internal sealed class RotateIcalTokenCommandHandler(
    IStudioAuthorization studioAuthorization,
    IArtistRepository artists) : IRequestHandler<RotateIcalTokenCommand, Result<RotateIcalTokenResult>>
{
    public async Task<Result<RotateIcalTokenResult>> Handle(RotateIcalTokenCommand request, CancellationToken cancellationToken)
    {
        var artistId = await studioAuthorization.GetCurrentArtistIdAsync(cancellationToken);
        if (artistId is null)
            return Result<RotateIcalTokenResult>.Failure(Error.Forbidden("Only artists have an iCal feed."));

        var artist = await artists.GetByIdAsync(artistId.Value, cancellationToken);
        if (artist is null)
            return Result<RotateIcalTokenResult>.Failure(Error.NotFound("Artist"));

        // 32 bytes → 43 url-safe base64 chars; comfortably below the 64-char column cap.
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        artist.IcalToken = token;
        return Result<RotateIcalTokenResult>.Success(new RotateIcalTokenResult(artist.Id, token));
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
