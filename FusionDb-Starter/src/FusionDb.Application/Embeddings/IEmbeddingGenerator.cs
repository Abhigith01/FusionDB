namespace FusionDb.Application.Embeddings;

public interface IEmbeddingGenerator
{
    Task<IReadOnlyList<float[]>> GenerateAsync(
        string model,
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default
    );
}
