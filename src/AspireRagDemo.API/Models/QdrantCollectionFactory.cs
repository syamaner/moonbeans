using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;
#pragma warning disable CS8603 // Possible null reference return.

namespace AspireRagDemo.API.Models;

public class QdrantCollectionFactory() : IQdrantVectorStoreRecordCollectionFactory
{
    public IVectorStoreRecordCollection<TKey, TRecord> CreateVectorStoreRecordCollection<TKey, TRecord>(QdrantClient qdrantClient, string name, VectorStoreRecordDefinition? vectorStoreRecordDefinition)
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
                    VectorStoreRecordDefinition = vectorStoreRecordDefinition
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