namespace FusionDb.Api.Contracts.Search;

public sealed record SearchCollectionResponse(
    string Query,
    int ResultCount,
    IReadOnlyList<SearchResultResponse> Results
);
