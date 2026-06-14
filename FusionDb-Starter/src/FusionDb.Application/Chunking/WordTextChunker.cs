using System.Text.RegularExpressions;

namespace FusionDb.Application.Chunking;

public sealed class WordTextChunker : ITextChunker
{
    private static readonly Regex WordPattern = new(
        @"\S+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    public IReadOnlyList<TextChunk> Chunk(string text, int chunkSize, int chunkOverlap)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text is required for chunking.", nameof(text));
        }

        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chunkSize),
                "Chunk size must be greater than zero."
            );
        }

        if (chunkOverlap < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(chunkOverlap),
                "Chunk overlap cannot be negative."
            );
        }

        if (chunkOverlap >= chunkSize)
        {
            throw new ArgumentException(
                "Chunk overlap must be smaller than chunk size.",
                nameof(chunkOverlap)
            );
        }

        var matches = WordPattern.Matches(text);

        if (matches.Count == 0)
        {
            return Array.Empty<TextChunk>();
        }

        var chunks = new List<TextChunk>();
        var startWordIndex = 0;
        var chunkNumber = 1;

        while (startWordIndex < matches.Count)
        {
            var endWordExclusive = Math.Min(startWordIndex + chunkSize, matches.Count);

            var firstWord = matches[startWordIndex];
            var finalWord = matches[endWordExclusive - 1];

            var startOffset = firstWord.Index;
            var endOffset = finalWord.Index + finalWord.Length;

            var content = text[startOffset..endOffset].Trim();

            chunks.Add(new TextChunk(chunkNumber, content, startOffset, endOffset));

            if (endWordExclusive >= matches.Count)
            {
                break;
            }

            startWordIndex = endWordExclusive - chunkOverlap;

            chunkNumber++;
        }

        return chunks;
    }
}
