#pragma warning disable SKEXP0070
using System.Diagnostics;
using AspireRagDemo.API.Chat;
using AspireRagDemo.ServiceDefaults;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;

namespace AspireRagDemo.API.Extensions;

public static class RagExtensions
{
    private const long HttpTimeoutMinutes= 10;
    public static void AddSemanticKernelModels(this WebApplicationBuilder builder)
    {
        var kernelBuilder = Kernel.CreateBuilder();
        AddVectorStore(builder, kernelBuilder);
        
        builder.Services.AddSingleton<ITechnicalAssistantChat, TechnicalAssistantChat>();
        
        var (embeddingHttpClient, embeddingModel) =
            GetHttpClientAndModelName(builder.Configuration, Constants.ConnectionStringNames.EmbeddingModel);
        var (chatHttpClient, chatModel) =
            GetHttpClientAndModelName(builder.Configuration, Constants.ConnectionStringNames.ChatModel);
        
        var kernel = kernelBuilder
            .AddOllamaTextEmbeddingGeneration(embeddingModel, embeddingHttpClient,
                Constants.ConnectionStringNames.EmbeddingModel)
            .AddOllamaChatCompletion(chatModel, chatHttpClient,
                Constants.ConnectionStringNames.ChatModel)
            .Build();
        builder.Services.AddSingleton(kernel);
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
        
        builder.Services.AddQdrantVectorStore(options:options);
        kernelBuilder.AddQdrantVectorStore(options:options);
    }

    private static (HttpClient, string) GetHttpClientAndModelName(IConfiguration configuration,
        string connectionStringName)
    {
        var connectionString = configuration.GetConnectionString(connectionStringName)
                               ?? throw new InvalidOperationException("Model connection string cannot be null.");
        var parts = connectionString.Split(";");
        var model = parts[1].Replace("Model=", "");
        Debug.Assert(connectionString != null, nameof(connectionString) + " != null");
 
        return (new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(HttpTimeoutMinutes),
            BaseAddress = new Uri(parts[0])
        }, model);
    }
}
 