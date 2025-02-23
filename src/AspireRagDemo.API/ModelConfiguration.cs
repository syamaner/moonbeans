using AspireRagDemo.API.Models;
using AspireRagDemo.ServiceDefaults;

namespace AspireRagDemo.API;
//TestConfiguration.HuggingFace.EmbeddingModelId
public class ModelConfiguration
{
    public string? OpenAiApiKey { get; set; }
    public string OllamaUrl { get; set; }
    /*public string VectorStoreCollectionName { get; set; } = null!;*/
    public string VectorStoreVectorName { get; set; } = null!;
    public List<BenchmarkConfiguration> BenchmarkConfigurations { get; set; } = null!;
}