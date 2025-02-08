using AspireRagDemo.ServiceDefaults;

namespace AspireRagDemo.API;
//TestConfiguration.HuggingFace.EmbeddingModelId
public class ModelConfiguration
{
    public string EmbeddingModel { get; set; } = null!;
    public ModelProvider EmbeddingModelProvider { get; set; }
    public string? EmbeddingModelProviderApiKey { get; set; }
    
    
    public string ChatModel { get; set; } = null!;
    public ModelProvider ChatModelProvider { get; set; }
    public string ChatModelProviderApiKey { get; set; } = null!;

    public string VectorStoreCollectionName { get; set; } = null!;
    public string VectorStoreVectorName { get; set; } = null!;
}