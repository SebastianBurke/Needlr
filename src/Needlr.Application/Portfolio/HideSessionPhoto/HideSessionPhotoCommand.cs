using Needlr.Application.Messaging;

namespace Needlr.Application.Portfolio.HideSessionPhoto;

/// <summary>
/// Artist hides a session photo. Per FEATURE_SPECS.md § Customer-uploaded photo policy this
/// is permitted ONLY for content-policy violations (NSFW, third-party PII, off-topic) — never
/// because the photo is unflattering or shows poor healing. <see cref="Reason"/> is required and
/// admin-auditable.
/// </summary>
public sealed record HideSessionPhotoCommand(Guid PhotoId, string Reason) : ICommand;
