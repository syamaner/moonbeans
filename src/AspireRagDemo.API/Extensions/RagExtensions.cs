#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0010
using AspireRagDemo.API.Chat;
using AspireRagDemo.API.Infrastructure;
using AspireRagDemo.ServiceDefaults;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Qdrant;

namespace AspireRagDemo.API.Extensions;

public static class RagExtensions
{
    private static readonly List<string> OpenAiModels = ["chatgpt-4o-latest", "text-embedding-3-large"];
    
    private const long HttpTimeoutMinutes = 10;
    public static void AddSemanticKernelModels(this WebApplicationBuilder builder)
    {
        var modelConfiguration = new ModelConfiguration();
        builder.Configuration.GetSection("ModelConfiguration").Bind(modelConfiguration);
        builder.Services.AddSingleton<IChatClient, ChatClient>();

        var kernelBuilder = Kernel.CreateBuilder();

       AddVectorStore(builder, modelConfiguration, kernelBuilder);
        
        AddEmbeddingModel(builder.Configuration, modelConfiguration, kernelBuilder);
        AddChatModel(builder.Configuration, modelConfiguration, kernelBuilder);
        
        var kernel = kernelBuilder.Build();
        builder.Services.AddSingleton(kernel);
    }

    private static void AddEmbeddingModel(IConfiguration configuration, ModelConfiguration modelConfiguration,
        IKernelBuilder kernelBuilder)
    {        
        var apiKey = modelConfiguration.OpenAiApiKey ?? throw new InvalidOperationException(
            $"Model Configuration {nameof(modelConfiguration.OpenAiApiKey)} cannot be null.");
             
        var embeddingModels = 
            modelConfiguration.BenchmarkConfigurations.Select(x => x.EmbeddingModel).Distinct();
        
        foreach (var model in embeddingModels)
        {
            if (OpenAiModels.Contains(model, StringComparer.OrdinalIgnoreCase))
            {
                kernelBuilder.AddOpenAITextEmbeddingGeneration(modelId: model,
                    apiKey: apiKey,
                    serviceId: model);
            }
            else
            {
                kernelBuilder.AddOllamaTextEmbeddingGeneration(model,
                    GetHttpClient(configuration, modelConfiguration.OllamaUrl),
                    model);
            }
        }
    }

    private static void AddChatModel(IConfiguration configuration, ModelConfiguration modelConfiguration,
        IKernelBuilder kernelBuilder)
    {        

        var models = modelConfiguration.BenchmarkConfigurations.SelectMany(x
            => x.ChatModels).Distinct();
        foreach (var model in models)
        {
            if ( OpenAiModels.Contains(model, StringComparer.OrdinalIgnoreCase))
            {
                var apiKey = modelConfiguration.OpenAiApiKey;
                kernelBuilder.AddOpenAIChatCompletion(model,
                    apiKey: apiKey,
                    serviceId: model);
            }
            else
            {
                kernelBuilder.AddOllamaChatCompletion(model,
                    GetHttpClient(configuration, modelConfiguration.OllamaUrl),
                    serviceId: model);                
            }
        }
    }

    private static void AddVectorStore(WebApplicationBuilder builder, ModelConfiguration modelConfiguration,
        IKernelBuilder kernelBuilder)
    {
        var configuration = builder.Configuration;
        var connectionString = configuration.GetConnectionString(Constants.ConnectionStringNames.Qdrant);
        var endpoint = connectionString?.Split(";")[0].Replace("Endpoint=", "");
        var key = connectionString?.Split(";")[1].Replace("Key=", "");

        var embeddingModels =
            modelConfiguration.BenchmarkConfigurations.Select(x => x.EmbeddingModel).Distinct();
        var parts = endpoint.Split(":");
        var url = parts[1].Replace("//", "");
        
        var port = int.Parse(parts[2]);
        foreach (var embeddingModel in embeddingModels)
        {
            // string host, int port = 6334, bool https = false, string? apiKey = default,
            var options = new QdrantVectorStoreOptions
            {
                HasNamedVectors = true,
                VectorStoreCollectionFactory = new QdrantCollectionFactory(embeddingModel)
            }; 

            builder.Services.AddQdrantVectorStore(options: options, host: url, port: port, apiKey: key,
                serviceId: embeddingModel);

            kernelBuilder.AddQdrantVectorStore(options: options, host: url, port: port, apiKey: key,
                serviceId: embeddingModel);
        }
    }

    private static HttpClient GetHttpClient(IConfiguration configuration, string ollamaUrl)
    { 
        return new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(HttpTimeoutMinutes),
            // API Project is not running in docker so we need to use localhost if using a locally hosted instance of Ollama.
            BaseAddress = new Uri(ollamaUrl)
        };
    }
}
 