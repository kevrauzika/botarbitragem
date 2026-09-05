namespace BotArbitragem.Application.Abstractions;

public interface IGroupNotifier
{
    Task SendAsync(string message, CancellationToken cancellationToken);
}

