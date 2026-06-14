using System.Text.Json;

namespace FusionDb.Api.Contracts.Search;

public sealed class SearchCollectionRequest
{
    public string Query { get; init; } = string.Empty;

    public int Limit { get; init; } = 5;

    public double MinimumSimilarity { get; init; } = 0.65;

    public JsonElement? MetadataFilter { get; init; }
}
