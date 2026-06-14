namespace FusionDb.Application.Chunking;

public sealed record TextChunk(
    int Number,
    string Content,
    int StartOffset,
    int EndOffset);