namespace AspireRagDemo.AppHost;

public record ChatConfiguration
{
    public ChatConfiguration()
    { 
        EmbeddingModel = Environment.GetEnvironmentVariable("EMBEDDING_MODEL") ?? "mxbai-embed-large";
        EmbeddingModelHostUri = Environment.GetEnvironmentVariable("EMBEDDING_MODEL_HOST_URI") ?? "";
        EmbeddingModelProvider = GetModelProvider(EmbeddingModelHostUri);
        
        ChatModel = Environment.GetEnvironmentVariable("CHAT_MODEL") ?? "phi3.5";
        ChatModelHostUri = Environment.GetEnvironmentVariable("CHAT_MODEL_HOST_URI") ?? "";
        ChatModelProvider = GetModelProvider(ChatModelHostUri);
        
        VectorStoreConnectionName =
            $"{StripSpecialCharacters(this.ChatModel)}{StripSpecialCharacters(this.EmbeddingModel)}";
    }
    
    private static ModelProvider  GetModelProvider(string? modelUri)
    { 
        if(modelUri==null)
            return ModelProvider.Ollama;
        
        if(modelUri.Contains("host.",StringComparison.InvariantCultureIgnoreCase)
            || modelUri.Contains("localhost",StringComparison.InvariantCultureIgnoreCase))
            return ModelProvider.OllamaHost;
        if(modelUri.Contains("hugging",StringComparison.InvariantCultureIgnoreCase))
            return ModelProvider.HuggingFace;
        
        return modelUri.Contains("openai",StringComparison.InvariantCultureIgnoreCase) 
            ? ModelProvider.OpenAI : ModelProvider.Ollama;
    }
    private static string StripSpecialCharacters(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return new string(input.Where(c =>
            c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '-').ToArray());
    }
    public string ChatModel { get; init; }
    public string ChatModelHostUri { get; init; }
    public ModelProvider ChatModelProvider { get; init; }
    public string EmbeddingModel { get; init; }
    public ModelProvider EmbeddingModelProvider { get; init; }
    public string EmbeddingModelHostUri { get; init; } 
    public string VectorStoreConnectionName { get; init; }
        
    public void Deconstruct(out string chatModel, out string chatModelHostUri, out ModelProvider chatModelProvider,
        out string embeddingModel, out string embeddingModelHostUri, out ModelProvider embeddingModelProvider,
        out string vectorStoreConnectionName)
    {
        chatModel = ChatModel;
        chatModelHostUri = ChatModelHostUri;
        chatModelProvider = ChatModelProvider;
        embeddingModel = EmbeddingModel;
        embeddingModelHostUri = EmbeddingModelHostUri;
        embeddingModelProvider = EmbeddingModelProvider;
        vectorStoreConnectionName = VectorStoreConnectionName;
    }
}