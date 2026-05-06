using Needlr.Application.Messaging;
using Needlr.Domain.Enums;

namespace Needlr.Application.MessageThreads.ResolveMessageReport;

public sealed record ResolveMessageReportCommand(
    Guid ReportId,
    MessageReportResolution Resolution) : ICommand;
