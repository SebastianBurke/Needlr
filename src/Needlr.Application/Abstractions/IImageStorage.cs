namespace Needlr.Application.Abstractions;

/// <summary>
/// Object storage for image blobs (portfolio photos, credential documents, message attachments).
/// Implementations: local filesystem for dev (writes to <c>wwwroot/uploads/</c>), Cloudflare R2
/// (S3-compatible) for prod. Per ADR-003, blob purges happen 1 year post-booking-terminal-state
/// via the Hangfire purge job; the DB record is retained with <c>Url</c> cleared.
/// </summary>
public interface IImageStorage
{
    /// <summary>
    /// Uploads <paramref name="content"/> to a generated key prefixed with <paramref name="keyPrefix"/>.
    /// Returns the key, which callers persist on the entity (<c>SessionPhoto.ImageUrl</c>,
    /// <c>BookingAttachment.Url</c>, etc.).
    /// </summary>
    Task<string> UploadAsync(
        Stream content,
        string contentType,
        string keyPrefix,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    Task<Stream> GetAsync(string key, CancellationToken cancellationToken = default);
}
