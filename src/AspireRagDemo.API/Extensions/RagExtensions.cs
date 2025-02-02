#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0010
using AspireRagDemo.API.Chat;
using AspireRagDemo.ServiceDefaults;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;

namespace AspireRagDemo.API.Extensions;

public static class RagExtensions
{
    private const long HttpTimeoutMinutes = 10;

    public static void AddSemanticKernelModels(this WebApplicationBuilder builder)
    {
        var modelConfiguration = new ModelConfiguration();
        builder.Configuration.GetSection("ModelConfiguration").Bind(modelConfiguration);
        builder.Services.AddSingleton<IChatClient, ChatClient>();

        var kernelBuilder = Kernel.CreateBuilder();

        AddVectorStore(builder, kernelBuilder);
        AddEmbeddingModel(builder.Configuration, modelConfiguration, kernelBuilder);
        AddChatModel(builder.Configuration, modelConfiguration, kernelBuilder);

        var kernel = kernelBuilder.Build();
        builder.Services.AddSingleton(kernel);
    }

    private static void AddEmbeddingModel(IConfiguration configuration, ModelConfiguration modelConfiguration,
        IKernelBuilder kernelBuilder)
    {
        
        var apiKey = modelConfiguration.EmbeddingModelProviderApiKey;
        var embeddingModel = modelConfiguration.EmbeddingModel;
        
        switch (modelConfiguration.EmbeddingModelProvider)
        {
            case ModelProvider.HuggingFace:
                kernelBuilder.AddHuggingFaceTextEmbeddingGeneration(model:embeddingModel,
                    apiKey:apiKey,
                    serviceId:Constants.ConnectionStringNames.EmbeddingModel);
                break;
            case ModelProvider.OpenAI:
                kernelBuilder.AddOpenAITextEmbeddingGeneration(modelId:embeddingModel,
                    apiKey:apiKey,
                    serviceId: Constants.ConnectionStringNames.EmbeddingModel);
                break;
            case ModelProvider.Ollama:
            case ModelProvider.OllamaHost:
                kernelBuilder.AddOllamaTextEmbeddingGeneration(modelConfiguration.EmbeddingModel,
                    GetHttpClient(configuration, Constants.ConnectionStringNames.EmbeddingModel),
                    Constants.ConnectionStringNames.EmbeddingModel);
                break;
            default:
                throw new ArgumentOutOfRangeException("EmbeddingModelProvider");
        }
    }

    private static void AddChatModel(IConfiguration configuration, ModelConfiguration modelConfiguration,
        IKernelBuilder kernelBuilder)
    { 
        var apiKey = modelConfiguration.ChatModelProviderApiKey;
        switch (modelConfiguration.ChatModelProvider)
        {
            case ModelProvider.HuggingFace:
                kernelBuilder.AddHuggingFaceChatCompletion(modelConfiguration.ChatModel,
                    apiKey: apiKey,
                    serviceId:Constants.ConnectionStringNames.ChatModel);
                break;
            case ModelProvider.OpenAI:
                kernelBuilder.AddOpenAIChatCompletion(modelConfiguration.ChatModel,
                    apiKey:apiKey,
                    serviceId: Constants.ConnectionStringNames.ChatModel);
                break;
            case ModelProvider.Ollama:
            case ModelProvider.OllamaHost:
                kernelBuilder.AddOllamaChatCompletion(modelConfiguration.ChatModel, 
                    GetHttpClient(configuration, Constants.ConnectionStringNames.ChatModel),
                    serviceId: Constants.ConnectionStringNames.ChatModel);
                break;
            default:
                throw new ArgumentOutOfRangeException("ChatModelProvider");
        }
    }

    private static void AddVectorStore(WebApplicationBuilder builder, IKernelBuilder kernelBuilder)
    {
        var configuration = builder.Configuration;
        var connectionString = configuration.GetConnectionString(Constants.ConnectionStringNames.Qdrant);
        var endpoint = connectionString?.Split(";")[0].Replace("Endpoint=", "");
        var key = connectionString?.Split(";")[1].Replace("Key=", "");
        var client = new QdrantClient(
            new Uri(endpoint ?? throw new InvalidOperationException("Qdrant endpoint cannot be null.")), key);
        builder.Services.AddSingleton(client);

        var options = new QdrantVectorStoreOptions
        {
            HasNamedVectors = true,
            VectorStoreCollectionFactory = new QdrantCollectionFactory()
        };
        builder.Services.AddSingleton(options);

        builder.Services.AddQdrantVectorStore(options: options);
        kernelBuilder.AddQdrantVectorStore(options: options);
    }

    private static HttpClient GetHttpClient(IConfiguration configuration, string connectionStringName)
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
                               ?? throw new InvalidOperationException("Model connection string cannot be null.");
        var parts = connectionString.Split(";");
        var host = parts[0].Replace("Endpoint=", "");

        return new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(HttpTimeoutMinutes),
            // API Project is not running in docker so we need to use localhost if using a locally hosted instance of Ollama.
            BaseAddress = new Uri(host.Replace("host.docker.internal", "localhost"))
        };
    }
}
 