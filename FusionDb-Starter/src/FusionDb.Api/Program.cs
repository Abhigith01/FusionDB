using FusionDb.Api.Endpoints;
using FusionDb.Application.Chunking;
using FusionDb.Application.Documents;
using FusionDb.Application.Embeddings;
using FusionDb.Application.Generation;
using FusionDb.Application.Observability;
using FusionDb.Application.Search;
using FusionDb.Infrastructure;
using FusionDb.Infrastructure.Documents;
using FusionDb.Infrastructure.Embeddings;
using FusionDb.Infrastructure.Generation;
using FusionDb.Infrastructure.Observability;
using FusionDb.Infrastructure.Search;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var ollamaBaseUrl =
    builder.Configuration["Ollama:BaseUrl"]
    ?? throw new InvalidOperationException("Ollama base URL is missing.");

builder.Services.AddHttpClient<IEmbeddingGenerator, OllamaEmbeddingGenerator>(client =>
{
    client.BaseAddress = new Uri(ollamaBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient<ITextGenerator, OllamaTextGenerator>(client =>
{
    client.BaseAddress = new Uri(ollamaBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(10);
});

builder.Services.AddScoped<IHybridSearchService, HybridSearchService>();

builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();

builder.Services.AddScoped<IRetrievalAuditService, RetrievalAuditService>();

builder.Services.AddHttpClient<ITextGenerator, OllamaTextGenerator>(client =>
{
    client.BaseAddress = new Uri(ollamaBaseUrl);
    client.Timeout = TimeSpan.FromMinutes(10);
});

var connectionString =
    builder.Configuration.GetConnectionString("FusionDb")
    ?? throw new InvalidOperationException("Connection string 'FusionDb' is missing.");

builder.Services.AddSingleton<ITextChunker, WordTextChunker>();

builder.Services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();

builder.Services.AddInfrastructure(connectionString);

builder.Services.AddSingleton(sp =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
    return dataSourceBuilder.Build();
});

var app = builder.Build();

app.MapGet(
    "/",
    () =>
        Results.Ok(
            new
            {
                name = "FusionDb",
                status = "running",
                description = "Hybrid operational and AI data platform",
            }
        )
);

app.MapGet(
    "/health/database",
    async (NpgsqlDataSource dataSource, CancellationToken cancellationToken) =>
    {
        try
        {
            await using var command = dataSource.CreateCommand(
                """
                SELECT
                    current_database() AS database_name,
                    current_setting('server_version') AS postgres_version,
                    EXISTS (
                        SELECT 1
                        FROM pg_extension
                        WHERE extname = 'vector'
                    ) AS vector_enabled;
                """
            );

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return Results.Problem(
                    title: "Database health check failed",
                    detail: "PostgreSQL returned no health-check row.",
                    statusCode: StatusCodes.Status503ServiceUnavailable
                );
            }

            return Results.Ok(
                new
                {
                    status = "healthy",
                    database = reader.GetString(0),
                    postgresVersion = reader.GetString(1),
                    vectorEnabled = reader.GetBoolean(2),
                }
            );
        }
        catch (Exception exception)
        {
            return Results.Problem(
                title: "Database health check failed",
                detail: exception.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable
            );
        }
    }
);

app.MapGet(
    "/health/vector",
    async (NpgsqlDataSource dataSource, CancellationToken cancellationToken) =>
    {
        await using var command = dataSource.CreateCommand(
            """
            SELECT
                '[1,0,0]'::vector <=> '[1,0,0]'::vector AS identical_distance,
                '[1,0,0]'::vector <=> '[0,1,0]'::vector AS different_distance;
            """
        );

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        await reader.ReadAsync(cancellationToken);

        return Results.Ok(
            new
            {
                status = "healthy",
                identicalVectorCosineDistance = reader.GetDouble(0),
                differentVectorCosineDistance = reader.GetDouble(1),
            }
        );
    }
);

app.MapCollectionEndpoints();
app.MapDocumentEndpoints();
app.MapSearchEndpoints();
app.MapAskEndpoints();

app.Run();
