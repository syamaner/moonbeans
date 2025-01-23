namespace AspireRagDemo.AppHost;
public enum ModelProvider
{
    Ollama=0,
    OpenAI=1,
    HuggingFace=2
}
public record ChatConfiguration
{
    public ChatConfiguration()
    { 
        this.ChatModel = Environment.GetEnvironmentVariable("CHAT_MODEL") ?? "phi3.5";
        this.EmbeddingModel = Environment.GetEnvironmentVariable("EMBEDDING_MODEL") ?? "mxbai-embed-large";
        this.ChatModelProvider = Enum.Parse<ModelProvider>(Environment.GetEnvironmentVariable("CHAT_MODEL_PROVIDER") ?? "Ollama");
        this.EmbeddingModelProvider = Enum.Parse<ModelProvider>(Environment.GetEnvironmentVariable("EMBEDDING_MODEL_PROVIDER") ?? "Ollama");
        
        this.VectorStoreConnectionName =
            $"{StripSpecialCharacters(this.ChatModel)}{StripSpecialCharacters(this.EmbeddingModel)}";
    }
    private static string StripSpecialCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        return new string(input.Where(c => 
            (c >= 'a' && c <= 'z') || 
            (c >= 'A' && c <= 'Z') || 
            c == '-').ToArray());
    }
    public string ChatModel { get; init; }
    public string EmbeddingModel { get; init; }
    public ModelProvider ChatModelProvider { get; init; }
    public ModelProvider EmbeddingModelProvider { get; init; }
    public string VectorStoreConnectionName { get; init; }

    public void Deconstruct(out string ChatModel, out string EmbeddingModel, out ModelProvider ChatModelProvider, out ModelProvider EmbeddingModelProvider, out string VectorStoreConnectionName)
    {
        ChatModel = this.ChatModel;
        EmbeddingModel = this.EmbeddingModel;
        ChatModelProvider = this.ChatModelProvider;
        EmbeddingModelProvider = this.EmbeddingModelProvider;
        VectorStoreConnectionName = this.VectorStoreConnectionName;
    }
}