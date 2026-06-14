using System.Net.Http.Json;
using FusionDb.Application.Embeddings;

namespace FusionDb.Infrastructure.Embeddings;

public sealed class OllamaEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient;

    public OllamaEmbeddingGenerator(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<float[]>> GenerateAsync(
        string model,
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            throw new ArgumentException("Embedding model is required.", nameof(model));
        }

        if (inputs.Count == 0)
        {
            return Array.Empty<float[]>();
        }

        var request = new OllamaEmbedRequest(model.Trim(), inputs);

        using var response = await _httpClient.PostAsJsonAsync(
            "/api/embed",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Ollama returned HTTP " + $"{(int)response.StatusCode}: {errorContent}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<OllamaEmbedResponse>(
            cancellationToken
        );

        if (result?.Embeddings is null)
        {
            throw new InvalidOperationException("Ollama returned no embeddings.");
        }

        if (result.Embeddings.Length != inputs.Count)
        {
            throw new InvalidOperationException(
                $"Ollama returned {result.Embeddings.Length} "
                    + $"embeddings for {inputs.Count} inputs."
            );
        }

        return result.Embeddings;
    }

    private sealed record OllamaEmbedRequest(string Model, IReadOnlyList<string> Input);

    private sealed record OllamaEmbedResponse(float[][] Embeddings);
}
