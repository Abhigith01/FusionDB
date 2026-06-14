namespace FusionDb.Application.Chunking;

public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(
        string text,
        int chunkSize,
        int chunkOverlap);
}