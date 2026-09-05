namespace BotArbitragem.Application.Abstractions;

public interface IGroupNotifier
{
    Task SendAsync(string message, CancellationToken cancellationToken);
    Task SendPollAsync(string question, IReadOnlyList<string> options, CancellationToken cancellationToken) =>
        throw new NotSupportedException("O notificador não oferece suporte a enquetes.");
}

