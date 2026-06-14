using System.Text.Json;

namespace FusionDb.Api.Contracts.Ask;

public sealed class AskCollectionRequest
{
    public string Question { get; init; } = string.Empty;

    public int MaxSources { get; init; } = 5;

    public double MinimumSimilarity { get; init; } = 0.65;

    public JsonElement? MetadataFilter { get; init; }
}
