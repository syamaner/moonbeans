using AspireRagDemo.ServiceDefaults;

namespace AspireRagDemo.API;
//TestConfiguration.HuggingFace.EmbeddingModelId
public class ModelConfiguration
{
    public string? EmbeddingModel { get; set; }
    public ModelProvider EmbeddingModelProvider { get; set; }
    public string? EmbeddingModelProviderApiKey { get; set; }
    
    
    public string? ChatModel { get; set; }
    public ModelProvider ChatModelProvider { get; set; }
    public string? ChatModelProviderApiKey { get; set; }

    public string? VectorStoreCollectionName { get; set; }
    public string? VectorStoreVectorName { get; set; }
}