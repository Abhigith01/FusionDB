namespace FusionDb.Application.Search;

public interface IHybridSearchService
{
    Task<HybridSearchResult> SearchAsync(
        Guid collectionId,
        string query,
        int limit,
        double minimumSimilarity,
        string? metadataFilterJson,
        CancellationToken cancellationToken = default
    );
}

public sealed record HybridSearchResult(bool CollectionFound, IReadOnlyList<HybridSearchHit> Hits);

public sealed record HybridSearchHit(
    Guid ChunkId,
    Guid DocumentId,
    string DocumentTitle,
    int ChunkNumber,
    string Content,
    double Distance,
    double Similarity,
    double KeywordScore,
    int? SemanticRank,
    int? KeywordRank,
    double HybridScore
);
