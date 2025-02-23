namespace AspireRagDemo.API.Models;

public class BenchmarkConfiguration
{
    public required string EmbeddingModel { get; set; }
    public required List<string> ChatModels { get; set; }
}
