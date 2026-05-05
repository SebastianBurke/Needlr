using Microsoft.Extensions.Options;
using Needlr.Application.Abstractions;

namespace Needlr.Infrastructure.Storage;

/// <summary>
/// Cloudflare R2 (S3-compatible) image storage. Stub: registered when
/// <c>ImageStorage:Backend = "R2"</c> but the actual S3 client wiring lands when AWSSDK.S3 is
/// added. Until then this throws on use, so misconfiguring the backend is loud rather than silent.
/// </summary>
internal sealed class R2ImageStorage : IImageStorage
{
    private readonly R2Options _options;

    public R2ImageStorage(IOptions<ImageStorageOptions> options)
    {
        _options = options.Value.R2
            ?? throw new InvalidOperationException(
                "ImageStorage:Backend is 'R2' but no ImageStorage:R2 settings are bound.");
    }

    public Task<string> UploadAsync(
        Stream content, string contentType, string keyPrefix, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException(
            "R2 backend not yet wired. Add AWSSDK.S3 to Needlr.Infrastructure and implement against " +
            $"{_options.EndpointUrl}.");

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("R2 backend not yet wired.");

    public Task<Stream> GetAsync(string key, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("R2 backend not yet wired.");
}
