using Microsoft.Extensions.Options;
using Needlr.Application.Abstractions;

namespace Needlr.Infrastructure.Storage;

/// <summary>
/// Filesystem-backed <see cref="IImageStorage"/> for development and tests. The returned key
/// is a forward-slash-separated relative path; <see cref="GetAsync"/> uses it directly to
/// resolve the file under <see cref="ImageStorageOptions.LocalRootPath"/>. Production uses
/// <c>R2ImageStorage</c> instead.
/// </summary>
internal sealed class LocalFilesystemImageStorage : IImageStorage
{
    private readonly string _root;

    public LocalFilesystemImageStorage(IOptions<ImageStorageOptions> options)
    {
        var opts = options.Value;
        _root = Path.GetFullPath(opts.LocalRootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> UploadAsync(
        Stream content,
        string contentType,
        string keyPrefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyPrefix);
        var prefix = keyPrefix.Trim('/').Replace('\\', '/');
        var key = $"{prefix}/{Guid.NewGuid():N}{GuessExtension(contentType)}";
        var fullPath = ToFullPath(key);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using (var fs = File.Create(fullPath))
        {
            await content.CopyToAsync(fs, cancellationToken);
        }

        return key;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullPath = ToFullPath(key);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task<Stream> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullPath = ToFullPath(key);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Image not found at key '{key}'.");
        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    private string ToFullPath(string key)
    {
        // Keys are forward-slash-separated, relative paths. Reject anything that escapes _root.
        var normalized = key.Replace('\\', '/').TrimStart('/');
        if (normalized.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("Key must not contain path-traversal segments.", nameof(key));
        var combined = Path.GetFullPath(Path.Combine(_root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        if (!combined.StartsWith(_root, StringComparison.Ordinal))
            throw new ArgumentException("Resolved path escapes the image-storage root.", nameof(key));
        return combined;
    }

    private static string GuessExtension(string contentType) => contentType switch
    {
        "image/jpeg" or "image/jpg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/heic" => ".heic",
        "application/pdf" => ".pdf",
        _ => ""
    };
}
