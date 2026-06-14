using System.Text.Json;
using FusionDb.Domain.Documents;

namespace FusionDb.Api.Contracts.Documents;

public sealed record AiDocumentResponse(
    Guid Id,
    Guid CollectionId,
    string Title,
    string Content,
    JsonElement Metadata,
    string ContentHash,
    string Status,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static AiDocumentResponse FromEntity(
        AiDocument document)
    {
        return new AiDocumentResponse(
            document.Id,
            document.CollectionId,
            document.Title,
            document.Content,
            JsonSerializer.Deserialize<JsonElement>(
                document.MetadataJson),
            document.ContentHash,
            document.Status.ToString(),
            document.FailureReason,
            document.CreatedAt,
            document.UpdatedAt);
    }
}