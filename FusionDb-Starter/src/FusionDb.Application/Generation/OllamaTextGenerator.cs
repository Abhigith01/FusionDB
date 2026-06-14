using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FusionDb.Application.Generation;

namespace FusionDb.Infrastructure.Generation;

public sealed class OllamaTextGenerator : ITextGenerator
{
    private readonly HttpClient _httpClient;

    public OllamaTextGenerator(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GenerateAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Generation model is required.", nameof(model));
        }

        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            throw new ArgumentException("System prompt is required.", nameof(systemPrompt));
        }

        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            throw new ArgumentException("User prompt is required.", nameof(userPrompt));
        }

        var request = new OllamaChatRequest(
            model.Trim(),
            new[]
            {
                new OllamaChatMessage("system", systemPrompt.Trim()),
                new OllamaChatMessage("user", userPrompt.Trim()),
            },
            false,
            new OllamaChatOptions(Temperature: 0, NumPredict: 500)
        );

        using var response = await _httpClient.PostAsJsonAsync(
            "/api/chat",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Ollama returned HTTP " + $"{(int)response.StatusCode}: {error}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            cancellationToken
        );

        var answer = result?.Message?.Content?.Trim();

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException("Ollama returned an empty answer.");
        }

        return answer;
    }

    private sealed record OllamaChatRequest(
        string Model,
        IReadOnlyList<OllamaChatMessage> Messages,
        bool Stream,
        OllamaChatOptions Options
    );

    private sealed record OllamaChatMessage(string Role, string Content);

    private sealed record OllamaChatOptions(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("num_predict")] int NumPredict
    );

    private sealed record OllamaChatResponse(OllamaChatMessage Message);
}
