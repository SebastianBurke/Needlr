using System.ComponentModel.DataAnnotations;

namespace Needlr.Infrastructure.Storage;

/// <summary>
/// Bound from the <c>ImageStorage</c> config section. Selects a backend and provides
/// backend-specific settings. Production sets <see cref="Backend"/> to <c>R2</c>; dev/tests
/// use <c>Local</c> (the default).
/// </summary>
public sealed class ImageStorageOptions
{
    public const string SectionName = "ImageStorage";

    [Required] public string Backend { get; set; } = ImageStorageBackend.Local;

    /// <summary>Local-filesystem root. Required when <see cref="Backend"/> = Local.</summary>
    public string LocalRootPath { get; set; } = "wwwroot/uploads";

    /// <summary>R2 (S3-compatible) settings — required when <see cref="Backend"/> = R2.</summary>
    public R2Options? R2 { get; set; }
}

public static class ImageStorageBackend
{
    public const string Local = "Local";
    public const string R2 = "R2";
}

public sealed class R2Options
{
    public string BucketName { get; set; } = null!;
    public string AccessKeyId { get; set; } = null!;
    public string SecretAccessKey { get; set; } = null!;
    public string EndpointUrl { get; set; } = null!;
    public string PublicBaseUrl { get; set; } = null!;
}
