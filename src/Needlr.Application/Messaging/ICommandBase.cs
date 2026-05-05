namespace Needlr.Application.Messaging;

/// <summary>
/// Non-generic marker for both <see cref="ICommand"/> and <see cref="ICommand{TResponse}"/>.
/// Used by <see cref="Behaviors.TransactionBehavior{TRequest,TResponse}"/> to detect requests
/// that should auto-commit on success.
/// </summary>
public interface ICommandBase;
