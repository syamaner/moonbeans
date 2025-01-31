using AspireRagDemo.ServiceDefaults;

namespace AspireRagDemo.AppHost;

public record ChatConfiguration
{
    public ChatConfiguration()
    { 
        EmbeddingModel = Environment.GetEnvironmentVariable("EMBEDDING_MODEL") ?? "mxbai-embed-large";
       
        if(Enum.TryParse<ModelProvider>(Environment.GetEnvironmentVariable("EMBEDDING_MODEL_PROVIDER"), 
            out var embeddingModelProvider))
        {
            EmbeddingModelProvider = embeddingModelProvider;
        }
 
        ChatModel = Environment.GetEnvironmentVariable("CHAT_MODEL") ?? "phi3.5";
        if(Enum.TryParse<ModelProvider>(Environment.GetEnvironmentVariable("CHAT_MODEL_PROVIDER"), 
               out var chatModelProvider))
        {
            ChatModelProvider = chatModelProvider;
        }
        
        VectorStoreVectorName = Environment.GetEnvironmentVariable("VECTOR_STORE_VECTOR_NAME") ?? "page_content_vector";
        VectorStoreCollectionName =
            NormaliseVectorStoreCollectionName($"{ChatModel}-{EmbeddingModel}");
    }
    
    /// <summary>
    /// Name of the Chat Model to be used.
    /// </summary>
    public string ChatModel { get; init; }    
    /// <summary>
    /// The URL for the chat model. If blank, Ollama via Aspire is implied.
    /// </summary>

    /// <summary>
    /// Whether or not using Ollama via Docker, Ollama on Host machine, OpenAI or Hugging Face.
    /// </summary>
    public ModelProvider ChatModelProvider { get; init; }
    /// <summary>
    /// Name of the Embedding Model to be used.
    /// </summary>
    public string EmbeddingModel { get; init; }
    /// <summary>
    /// Whether or not using Ollama via Docker, Ollama on Host machine, OpenAI or Hugging Face.
    /// </summary>
    public ModelProvider EmbeddingModelProvider { get; init; }

    /// <summary>
    /// This value is passed down to our API Project as well as the Python code using Jupyter Notebooks.
    /// </summary>
    public string VectorStoreCollectionName { get; init; }
    
    public string VectorStoreVectorName { get; init; }
         
    /// <summary>
    /// We are generating Vector Store Collection name depenmding on chat and embedding model in use.
    /// For Qdrant, there are certain naming conventions that need to be followed. So we strip out all excect letters (upper / lower) and hyphens.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static string NormaliseVectorStoreCollectionName(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return new string(input.Where(c =>
            c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or '-').ToArray());
    }
}