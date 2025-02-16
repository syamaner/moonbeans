using AspireRagDemo.API.Models;
using AspireRagDemo.ServiceDefaults.Metrics;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001

namespace AspireRagDemo.API.Ingestion;

public class IngestionPipeline(
    Kernel kernel,
    IVectorStore vectorStore,
    AspireRagDemoIngestionMetrics metrics,
    IOptions<ModelConfiguration> configuration,
    ILogger<IngestionPipeline> logger,
    IEnumerable<IChunker> documentChunkers)
{
    private readonly IVectorStoreRecordCollection<Guid, FaqRecord> _faqCollection =
        vectorStore.GetCollection<Guid, FaqRecord>(configuration.Value.VectorStoreCollectionName ??
                                                   throw new InvalidOperationException(
                                                       $"Vector store collection name is not set in the configuration. {configuration.Value.VectorStoreCollectionName}"));
    private readonly ITextEmbeddingGenerationService _embeddingGenerator =
        kernel.GetRequiredService<ITextEmbeddingGenerationService>();

    public async Task IngestDataAsync(string filePath, DocumentType documentType)
    {
        await EnsureCollectionExists(true);
        var documentsProcessed = 0;
        var documentChunker = documentChunkers.FirstOrDefault(x => x.CanChunk(documentType));

        if (documentChunker == null)
        {
            logger.LogError("No chunker found for {DOCUMENTTYPE}", documentType);
            throw new ArgumentException($"Document type {documentType} is not supported.");
        }

        using var ingestionTimer = new MetricTimer(metrics,
            MetricNames.DocumentIngestion, new KeyValuePair<string, object?>("File", filePath),
            new KeyValuePair<string, object?>("EmbeddingModel", configuration.Value.EmbeddingModel));

        await foreach (var fileChunk in documentChunker.GetChunks(filePath))
        {
            try
            {
                IList<ReadOnlyMemory<float>>? embeddings = null;

                using (new MetricTimer(metrics,
                           MetricNames.Embedding, new KeyValuePair<string, object?>("File", filePath),
                           new KeyValuePair<string, object?>("EmbeddingModel", configuration.Value.EmbeddingModel)))
                {
                    embeddings = await _embeddingGenerator.GenerateEmbeddingsAsync(fileChunk.Chunks);
                }

                metrics.RecordProcessedChunkCount(fileChunk.Chunks.Count);
                for (var i = 0; i < fileChunk.Chunks.Count; i++)
                {
                    try
                    {
                        var faqRecord = new FaqRecord()
                        {
                            Id = Guid.NewGuid(),
                            Content = fileChunk.Chunks[i],
                            Vector = embeddings[i],
                            Metadata = new FileMetadata()
                            {
                                FileName = new StringValue() { Value = fileChunk.FileName }
                            }
                        };
                        await _faqCollection.UpsertAsync(faqRecord);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Error inserting the vectors into vector store.");
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error generating embeddings for the file {FILENAME}", fileChunk.FileName);
            }

            documentsProcessed++;
        }

        metrics.RecordProcessedDocumentCount(documentsProcessed);
    }

    private async Task EnsureCollectionExists(bool forceRecreate = false)
    {
        var collectionExists = await _faqCollection.CollectionExistsAsync();
        switch (collectionExists)
        {
            case true when !forceRecreate:
                return;
            case true:
                await _faqCollection.DeleteCollectionAsync();
                break;
        }

        await _faqCollection.CreateCollectionAsync();
    }
}