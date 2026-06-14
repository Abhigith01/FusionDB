namespace FusionDb.Application.Documents;

public interface IPdfTextExtractor
{
    Task<PdfTextExtractionResult> ExtractAsync(
        Stream stream,
        CancellationToken cancellationToken = default
    );
}

public sealed record PdfTextExtractionResult(string Text, int PageCount);
