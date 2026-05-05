using Needlr.Application.Messaging;

namespace Needlr.Application.Portfolio.UploadHealedPhoto;

/// <summary>
/// Customer uploads a healed photo for one of their completed bookings. Per
/// FEATURE_SPECS.md § Portfolio > Photo handling, the artist must have already created the
/// linked portfolio piece (with the Fresh photo); this command appends a Healed
/// <c>SessionPhoto</c> to that piece, attributed to the customer.
/// </summary>
public sealed record UploadHealedPhotoCommand(
    Guid BookingId,
    Stream FileContent,
    string ContentType,
    string OriginalFilename) : ICommand<Guid>;
