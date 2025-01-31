using Microsoft.Extensions.VectorData;

namespace AspireRagDemo.API.Models;

public class FaqRecord
{    
    [VectorStoreRecordKey]
    public Guid Id { get; set; }
    
    [VectorStoreRecordData(IsFilterable = true, StoragePropertyName = "page_content")]
    public required string Content { get; set; }
    
    [VectorStoreRecordData(IsFullTextSearchable = true, StoragePropertyName = "metadata")]
    public required FileMetadata? Metadata { get; set; }
    
    [VectorStoreRecordVector(768, DistanceFunction.CosineDistance, IndexKind.Hnsw, StoragePropertyName = "page_content_vector")]
    public ReadOnlyMemory<float>? Vector { get; set; }
}