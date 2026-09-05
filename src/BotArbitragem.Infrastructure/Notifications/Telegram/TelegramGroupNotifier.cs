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

    public async Task SendPollAsync(
        string question,
        IReadOnlyList<string> options,
        CancellationToken cancellationToken)
    {
        var settings = GetValidatedSettings();
        if (string.IsNullOrWhiteSpace(question) || question.Length > 300)
            throw new ArgumentException("A pergunta deve conter entre 1 e 300 caracteres.", nameof(question));
        if (options.Count is < 2 or > 10 || options.Any(option => string.IsNullOrWhiteSpace(option)))
            throw new ArgumentException("A enquete deve conter de 2 a 10 opções.", nameof(options));

        var payload = new SendPollRequest(
            settings.ChatId,
            question,
            options.Select(option => new PollOption(option)).ToArray(),
            settings.MessageThreadId,
            settings.DisableNotification);

        await PostAsync(settings.BotToken, "sendPoll", payload, cancellationToken);
    }

    private TelegramOptions GetValidatedSettings()
    {
        var settings = options.Value;
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.BotToken) || string.IsNullOrWhiteSpace(settings.ChatId))
            throw new InvalidOperationException("A integração com o Telegram não está configurada.");
        return settings;
    }

    private async Task PostAsync<T>(string token, string method, T payload, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                $"bot{Uri.EscapeDataString(token)}/{method}", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new GroupNotificationException($"O Telegram respondeu com status {(int)response.StatusCode}.");
        }
        catch (GroupNotificationException) { throw; }
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
    private sealed record SendPollRequest(
        [property: JsonPropertyName("chat_id")] string ChatId,
        [property: JsonPropertyName("question")] string Question,
        [property: JsonPropertyName("options")] IReadOnlyList<PollOption> Options,
        [property: JsonPropertyName("message_thread_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? MessageThreadId,
        [property: JsonPropertyName("disable_notification")] bool DisableNotification,
        [property: JsonPropertyName("is_anonymous")] bool IsAnonymous = false,
        [property: JsonPropertyName("allows_multiple_answers")] bool AllowsMultipleAnswers = false);
    private sealed record PollOption([property: JsonPropertyName("text")] string Text);
}

