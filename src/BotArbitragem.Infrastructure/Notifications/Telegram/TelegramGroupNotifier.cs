using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace BotArbitragem.Infrastructure.Notifications.Telegram;

public sealed class TelegramGroupNotifier(HttpClient httpClient, IOptions<TelegramOptions> options) : IGroupNotifier
{
    public async Task SendAsync(string message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.BotToken) || string.IsNullOrWhiteSpace(settings.ChatId))
            throw new InvalidOperationException("A integração com o Telegram não está configurada.");
        if (string.IsNullOrWhiteSpace(message) || message.Length > 4096)
            throw new ArgumentException("A mensagem deve conter entre 1 e 4096 caracteres.", nameof(message));

        var payload = new SendMessageRequest(
            settings.ChatId,
            message,
            settings.MessageThreadId,
            settings.DisableNotification,
            true);

        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"bot{Uri.EscapeDataString(settings.BotToken)}/sendMessage",
                payload,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new GroupNotificationException($"O Telegram respondeu com status {(int)response.StatusCode}.");
        }
        catch (GroupNotificationException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GroupNotificationException("O Telegram excedeu o tempo limite de resposta.");
        }
        catch (HttpRequestException)
        {
            throw new GroupNotificationException("Não foi possível conectar ao Telegram.");
        }
    }

    private sealed record SendMessageRequest(
        [property: JsonPropertyName("chat_id")] string ChatId,
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("message_thread_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? MessageThreadId,
        [property: JsonPropertyName("disable_notification")] bool DisableNotification,
        [property: JsonPropertyName("protect_content")] bool ProtectContent);
}

