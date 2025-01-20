namespace AspireRagDemo.AppHost;
public enum ModelProvider
{
    Ollama=0,
    OpenAI=1,
    HuggingFace=2
}
public record ChatConfiguration(string ChatModel, string EmbeddingModel, ModelProvider ChatModelProvider, ModelProvider EmbeddingModelProvider);