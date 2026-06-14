using System.Text.Json;

namespace FusionDb.Api.Contracts.Documents;

public sealed record CreateAiDocumentRequest(
    string Title,
    string Content,
    JsonElement? Metadata);