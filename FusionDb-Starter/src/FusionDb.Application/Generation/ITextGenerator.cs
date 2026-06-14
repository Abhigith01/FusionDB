namespace FusionDb.Application.Generation;

public interface ITextGenerator
{
    Task<string> GenerateAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default
    );
}
