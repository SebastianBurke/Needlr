using Needlr.Application.Common.Results;
using Needlr.Application.Messaging;

namespace Needlr.Application.MessageThreads.GetUnreadCount;

public sealed record GetUnreadMessageCountQuery : IQuery<int>;
