#pragma warning disable SKEXP0070
using System.Diagnostics;
using AspireRagDemo.API.Chat;
using AspireRagDemo.ServiceDefaults;
using Microsoft.SemanticKernel;
using Qdrant.Client;

namespace AspireRagDemo.API.Extensions;

public static class RagExtensions
{
    public static void AddSemanticKernelModels(this WebApplicationBuilder builder)
    {
        var kernelBuilder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<ITechnicalAssistantChat, TechnicalAssistantChat>();
        builder.Services.AddSingleton<Kernel>(sp =>
        {
            
            var configuration = sp.GetRequiredService<IConfiguration>();
            var (embeddingHttpClient, embeddingModel) =
                GetHttpClientAndModelName(configuration, Constants.ConnectionStringNames.EmbeddingModel);
            var (chatHttpClient, chatModel) = 
                GetHttpClientAndModelName(configuration, Constants.ConnectionStringNames.ChatModel);

            var kernel = kernelBuilder
                .AddOllamaTextEmbeddingGeneration(embeddingModel, embeddingHttpClient,
                    Constants.ConnectionStringNames.EmbeddingModel.ToString())
                .AddOllamaChatCompletion(chatModel, chatHttpClient,
                    Constants.ConnectionStringNames.ChatModel.ToString())
                .Build();
            return kernel;
        });
    }

    public static void AddVectorStore(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<QdrantClient>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(Constants.ConnectionStringNames.Qdrant);
            var endpoint = connectionString?.Split(";")[0].Replace("Endpoint=", "");
            var key = connectionString?.Split(";")[1].Replace("Key=", "");

            return new QdrantClient(
                new Uri(endpoint ?? throw new InvalidOperationException("Qdrant endpoint cannot be null.")), key);
        });
        builder.Services.AddQdrantVectorStore();
    }

    private static (HttpClient, string) GetHttpClientAndModelName(IConfiguration configuration, string connectionStringName)
    { 
        var connectionString = configuration.GetConnectionString(connectionStringName);
        var model = connectionString.Split(";")[1].Replace("Model=", "");
        Debug.Assert(connectionString != null, nameof(connectionString) + " != null");
        var uri = new Uri(connectionString.Split(";")[0].Replace("Endpoint=", ""));
        return (new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10),
            BaseAddress = uri
        }, model);
    }
}