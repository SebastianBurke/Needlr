using Needlr.Application.Messaging;

namespace Needlr.Application.Verification.GetVerificationQueue;

public sealed record GetVerificationQueueQuery() : IQuery<IReadOnlyList<VerificationQueueItemDto>>;
