using AspireRagDemo.API.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;

#pragma warning disable CS8603 // Possible null reference return.

namespace AspireRagDemo.API.Infrastructure;

public class QdrantCollectionFactory(string embeddingModel="nomic-embed-text") : IQdrantVectorStoreRecordCollectionFactory
{
    private static readonly Dictionary<string, int> EmbeddingModels = new()
    {
        { "mxbai-embed-large", 1024 },
        { "nomic-embed-text", 768 },
        { "granite-embedding:30m", 384 }
    };


    private readonly VectorStoreRecordDefinition _faqRecordDefinition = new()
    {
        Properties = new List<VectorStoreRecordProperty>
        {
            new VectorStoreRecordKeyProperty("Id", typeof(Guid)),
            new VectorStoreRecordDataProperty("Content",
                typeof(string)) { IsFilterable = true, StoragePropertyName = "page_content" },
            new VectorStoreRecordDataProperty("Metadata", typeof(FileMetadata))
            {
                IsFullTextSearchable = false, StoragePropertyName = "metadata"
            },
            new VectorStoreRecordVectorProperty("Vector", typeof(float))
            {
                Dimensions = EmbeddingModels.ContainsKey(embeddingModel) ? EmbeddingModels[embeddingModel] : 384,
                DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw,
                StoragePropertyName = "page_content_vector"
            },
        }
    };
    
    public IVectorStoreRecordCollection<TKey, TRecord> CreateVectorStoreRecordCollection<TKey, TRecord>(
        QdrantClient qdrantClient, string name, VectorStoreRecordDefinition? vectorStoreRecordDefinition)
        where TKey : notnull
    {

        if ( typeof(TRecord) == typeof(FaqRecord))
        {
            var customCollection = new QdrantVectorStoreRecordCollection<FaqRecord>(
                qdrantClient,
                name,
                new QdrantVectorStoreRecordCollectionOptions<FaqRecord>
                {
                    HasNamedVectors = true,
                    PointStructCustomMapper = new FaqRecordMapper(),
                    VectorStoreRecordDefinition = _faqRecordDefinition //vectorStoreRecordDefinition
                }) as IVectorStoreRecordCollection<TKey, TRecord>;
            return customCollection;
        }

        // Otherwise, just create a standard collection with the default mapper.
        var collection = new QdrantVectorStoreRecordCollection<TRecord>(
            qdrantClient,
            name,
            new QdrantVectorStoreRecordCollectionOptions<TRecord>
            {
                VectorStoreRecordDefinition = vectorStoreRecordDefinition
            }) as IVectorStoreRecordCollection<TKey, TRecord>;
        return collection;
    }
}