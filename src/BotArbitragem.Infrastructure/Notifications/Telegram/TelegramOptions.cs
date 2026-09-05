namespace BotArbitragem.Infrastructure.Notifications.Telegram;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";
    public bool Enabled { get; init; }
    public string BotToken { get; init; } = string.Empty;
    public string ChatId { get; init; } = string.Empty;
    public int? MessageThreadId { get; init; }
    public bool DisableNotification { get; init; }
}

