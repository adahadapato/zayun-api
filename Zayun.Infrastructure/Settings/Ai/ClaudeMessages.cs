using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Zayun.Infrastructure.Settings.Ai;

/// <summary>
/// The one connection to Claude (the Anthropic Messages API) that every AI helper uses: the tag check, the quote helper
/// and the helpers still to come. It sends the request, adds the key and headers, waits at most
/// <see cref="AiSettings.TimeoutSeconds"/>, checks the answer and writes the same log lines for every helper.
/// </summary>
/// <remarks>
/// <para>Each helper only brings what is its own: its instructions, the question, and the JSON shape it wants back
/// (structured outputs). It gets back the answer's JSON text and reads it itself.</para>
/// <para>Never throws for the AI: no key, a timeout, an error status, a refusal or an unreadable answer is logged and
/// returns null, so each helper can carry on without the AI.</para>
/// <para>Log lines start with "AI {helper}", for example <c>AI quote helper timed out after 20s.</c> or
/// <c>AI tag check answered in 1840 ms.</c></para>
/// </remarks>
internal sealed class ClaudeMessages(HttpClient http, IOptions<AiSettings> options, ILogger<ClaudeMessages> logger)
{
    private const string AnthropicVersion = "2023-06-01";

    /// <summary>JSON settings for the request and for reading answers (camelCase, case-insensitive).</summary>
    internal static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>True when the AI can be used (switched on, with a key).</summary>
    public bool IsAvailable => options.Value.IsUsable;

    /// <summary>Asks Claude one question and gets back JSON in the given shape.</summary>
    /// <param name="helper">The helper's name for the log, e.g. "tag check" or "quote helper".</param>
    /// <param name="instructions">The helper's standing instructions (the system prompt).</param>
    /// <param name="question">This request's details (the user message).</param>
    /// <param name="schema">The JSON shape the answer must have (a JSON Schema object).</param>
    /// <param name="maxTokens">The longest answer allowed.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The answer's JSON text, or null when there's no usable answer (already logged).</returns>
    public async Task<string?> AskForJsonAsync(string helper, string instructions, string question, object schema,
                                               int maxTokens, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.IsUsable) return null;

        var request = new MessagesRequest(
            Model: settings.Model,
            MaxTokens: maxTokens,
            System: instructions,
            Messages: [new Message("user", question)],
            OutputConfig: new OutputConfig(new OutputFormat("json_schema", schema)));

        var started = Stopwatch.GetTimestamp();
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));

            using var message = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
            {
                Content = JsonContent.Create(request, options: Json)
            };
            message.Headers.Add("x-api-key", settings.ApiKey);
            message.Headers.Add("anthropic-version", AnthropicVersion);

            using var response = await http.SendAsync(message, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(timeout.Token);
                logger.LogWarning("AI {Helper} failed: {Status} {Body}", helper, (int)response.StatusCode,
                                  body.Length > 500 ? body[..500] : body);
                return null;
            }

            var answer = await response.Content.ReadFromJsonAsync<MessagesResponse>(Json, timeout.Token);
            var elapsedMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;

            if (answer is null || answer.StopReason is "refusal" or "max_tokens")
            {
                logger.LogWarning("AI {Helper} gave no usable answer ({StopReason}) after {Ms} ms.",
                                  helper, answer?.StopReason, elapsedMs);
                return null;
            }

            var text = answer.Content?.FirstOrDefault(c => c.Type == "text")?.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogWarning("AI {Helper} answered without text after {Ms} ms.", helper, elapsedMs);
                return null;
            }

            logger.LogInformation("AI {Helper} answered in {Ms} ms.", helper, elapsedMs);
            return text;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("AI {Helper} timed out after {Seconds}s.", helper, settings.TimeoutSeconds);
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException)
        {
            logger.LogWarning(ex, "AI {Helper} failed.", helper);
            return null;
        }
    }

    // ───────────────────────────── Request and answer shapes ─────────────────────────────

    private sealed record MessagesRequest(
        string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        string System,
        IReadOnlyList<Message> Messages,
        [property: JsonPropertyName("output_config")] OutputConfig OutputConfig);

    private sealed record Message(string Role, string Content);

    private sealed record OutputConfig(OutputFormat Format);

    private sealed record OutputFormat(string Type, object Schema);

    private sealed record MessagesResponse(
        IReadOnlyList<ContentBlock>? Content,
        [property: JsonPropertyName("stop_reason")] string? StopReason);

    private sealed record ContentBlock(string Type, string? Text);
}
