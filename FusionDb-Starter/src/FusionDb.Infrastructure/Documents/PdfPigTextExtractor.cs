using System.Text;
using FusionDb.Application.Documents;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace FusionDb.Infrastructure.Documents;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public async Task<PdfTextExtractionResult> ExtractAsync(
        Stream stream,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(stream);

        await using var memoryStream = new MemoryStream();

        await stream.CopyToAsync(memoryStream, cancellationToken);

        var fileBytes = memoryStream.ToArray();

        using var document = PdfDocument.Open(fileBytes);

        var textBuilder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var pageText = ContentOrderTextExtractor.GetText(page);

            if (string.IsNullOrWhiteSpace(pageText))
            {
                continue;
            }

            if (textBuilder.Length > 0)
            {
                textBuilder.AppendLine();
                textBuilder.AppendLine();
            }

            textBuilder.Append(pageText.Trim());
        }

        return new PdfTextExtractionResult(textBuilder.ToString().Trim(), document.NumberOfPages);
    }
}
