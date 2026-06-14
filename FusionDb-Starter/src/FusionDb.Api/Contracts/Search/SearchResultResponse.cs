namespace FusionDb.Api.Contracts.Search;

public sealed record SearchResultResponse(
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
