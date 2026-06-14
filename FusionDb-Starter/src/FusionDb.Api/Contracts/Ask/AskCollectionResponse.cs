namespace FusionDb.Api.Contracts.Ask;

public sealed record AskCollectionResponse(
    string Question,
    string Answer,
    bool Grounded,
    IReadOnlyList<AskSourceResponse> Sources
);

public sealed record AskSourceResponse(
    string Citation,
    Guid ChunkId,
    Guid DocumentId,
    string DocumentTitle,
    int ChunkNumber,
    double Similarity,
    double HybridScore,
    string Content
);
