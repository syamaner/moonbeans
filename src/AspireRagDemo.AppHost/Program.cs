using AspireRagDemo.AppHost;
using AspireRagDemo.AppHost.Extensions;
using AspireRagDemo.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);
const string ollamaHostConnectionString = "Endpoint=http://host.docker.internal:11434";

ChatConfiguration chatConfiguration = new();
// this will be used for evaluating our performance in the evaluation notebook
// Ingestion and Query will use Ollama for both embeddings and generation.
var openAiKey = builder.AddParameter("OpenAIKey", secret: true);
var hfApiKey = builder.AddParameter("HuggingFaceKey", secret: true);

// container ports we will be using
Dictionary<string, int> applicationPorts = new()
{
    { Constants.ConnectionStringNames.ApiService, 5000 },
    { Constants.ConnectionStringNames.Ui, 8501 },
    { Constants.ConnectionStringNames.JupyterService, 8888 }
};

var vectorStore = builder.AddQdrant(Constants.ConnectionStringNames.Qdrant)
    .WithImageTag("v1.13.0-unprivileged")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithBindMount( "./data/qdrant","/qdrant/storage"); //if using Podman on windows, this might be necessary :z

// It is possible to use a model provider as following:
//  - Ollama using Aspire (might be good option if using an Nvidia Docker Compatible Docker host.
//  - Ollama on  host machine. Could be an option if using something like
//       a MacBook Pro with dedicated GPU where such features are not supported in Docker natively
//  - Also possible to use Open AI and Hugging Faces too.
// Downstream projects will resect the choices too so we only make the changes in app host configuration.
builder.AddModelProvider(chatConfiguration, 
    out var chatModel, 
    out var embeddingModel);

var apiService = builder.AddProject<Projects.AspireRagDemo_API>(Constants.ConnectionStringNames.ApiService)    
    .WithEnvironment("VectorStoreCollectionName", chatConfiguration.VectorStoreCollectionName)
    .WithEnvironment("EMBEDDING_MODEL",chatConfiguration.EmbeddingModel)
    .WithEnvironment("CHAT_MODEL",chatConfiguration.ChatModel)
    .WithEnvironment("ModelConfiguration__EmbeddingModel",chatConfiguration.EmbeddingModel)
    .WithEnvironment("ModelConfiguration__EmbeddingModelProvider",chatConfiguration.EmbeddingModelProvider.ToString)
    .WithEnvironment("ModelConfiguration__EmbeddingModelProviderApiKey",GetApiProviderKey(chatConfiguration.EmbeddingModelProvider))
    .WithEnvironment("ModelConfiguration__ChatModel",chatConfiguration.ChatModel)
    .WithEnvironment("ModelConfiguration__ChatModelProvider",chatConfiguration.ChatModelProvider.ToString)
    .WithEnvironment("ModelConfiguration__ChatModelProviderApiKey",GetApiProviderKey(chatConfiguration.ChatModelProvider))
    .WithEnvironment("ModelConfiguration__VectorStoreCollectionName",chatConfiguration.VectorStoreCollectionName)
    .WithEnvironment("ModelConfiguration__VectorStoreVectorName",chatConfiguration.VectorStoreVectorName)
    .WithReference(vectorStore)
    .WaitFor(vectorStore) ;
    
// For UI, we will use StreamLit and run as a container.
_ = builder
    .AddDockerfile(Constants.ConnectionStringNames.Ui, "../AspireRagDemo.UI")
    .WithBuildArg("PORT", applicationPorts[Constants.ConnectionStringNames.Ui])
    .WithHttpEndpoint(targetPort: applicationPorts[Constants.ConnectionStringNames.Ui], env: "PORT")
    .WithReference(apiService)
    .WaitFor(apiService)    
    .WithOtlpExporter()
    .WithExternalHttpEndpoints();


// For the ingestion pipeline and evaluation, we will be using Python and Jupyter.
var jupyter = builder
    .AddDockerfile(Constants.ConnectionStringNames.JupyterService, "./Jupyter")
    .WithBuildArg("PORT", applicationPorts[Constants.ConnectionStringNames.JupyterService])    
    .WithArgs($"--NotebookApp.token=''")
    .WithBindMount("./Jupyter/Notebooks/","/home/jovyan/work")
    .WithHttpEndpoint(targetPort: applicationPorts[Constants.ConnectionStringNames.JupyterService], env: "PORT")
    .WithLifetime(ContainerLifetime.Session)
    .WithOtlpExporter()
    .WithEnvironment("OTEL_SERVICE_NAME","jupyterdemo")
    .WithEnvironment("OTEL_EXPORTER_OTLP_INSECURE","true")
    .WithEnvironment("PYTHONUNBUFFERED", "0")
    .WithEnvironment("OPENAI_API_KEY", openAiKey.Resource.Value)
    .WithEnvironment("VECTOR_STORE_COLLECTION_NAME", chatConfiguration.VectorStoreCollectionName)
    .WithEnvironment("EMBEDDING_MODEL",chatConfiguration.EmbeddingModel)
    .WithEnvironment("CHAT_MODEL",chatConfiguration.ChatModel)
    .WithEnvironment("ModelConfiguration__EmbeddingModel",chatConfiguration.EmbeddingModel)
    .WithEnvironment("ModelConfiguration__EmbeddingModelProvider",chatConfiguration.EmbeddingModelProvider.ToString)
    .WithEnvironment("ModelConfiguration__EmbeddingModelProviderApiKey",GetApiProviderKey(chatConfiguration.EmbeddingModelProvider))
    .WithEnvironment("ModelConfiguration__ChatModel",chatConfiguration.ChatModel)
    .WithEnvironment("ModelConfiguration__ChatModelProvider",chatConfiguration.ChatModelProvider.ToString)
    .WithEnvironment("ModelConfiguration__ChatModelProviderApiKey",GetApiProviderKey(chatConfiguration.ChatModelProvider))
    .WithEnvironment("ModelConfiguration__VectorStoreCollectionName",chatConfiguration.VectorStoreCollectionName)
    .WithEnvironment("ModelConfiguration__VectorStoreVectorName",chatConfiguration.VectorStoreVectorName)
    .WithReference(vectorStore)
    .WithReference(apiService)
    .WaitFor(vectorStore)
    .WaitFor(apiService)
    .WithExternalHttpEndpoints();

// Verify if we have spun up an Ollama server using aspire or not.
// if not, we will inject the connection string as Aspire does but actual resource could be running anywhere.
// This allows to also use commercial Services if we wanted and ensure our system components are consistent.
if(chatModel != null)
{
    apiService.WithReference(chatModel).WaitFor(chatModel);
    jupyter.WithReference(chatModel).WaitFor(chatModel);
}
else
{
    var connectionString = chatConfiguration.ChatModelProvider switch
    {
        ModelProvider.Ollama or ModelProvider.OllamaHost => ollamaHostConnectionString,
        ModelProvider.OpenAI or ModelProvider.HuggingFace => string.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(chatConfiguration.ChatModelProvider),
            chatConfiguration.ChatModelProvider, null)
    };
    apiService.WithEnvironment("ConnectionStrings__chat-model", connectionString);
    jupyter.WithEnvironment("ConnectionStrings__chat-model", connectionString);
}

if(embeddingModel!=null)
{
    apiService.WithReference(embeddingModel).WaitFor(embeddingModel);
    jupyter.WithReference(embeddingModel).WaitFor(embeddingModel);
}
else
{
    var connectionString = chatConfiguration.ChatModelProvider switch
    {
        ModelProvider.Ollama or ModelProvider.OllamaHost => ollamaHostConnectionString,
        ModelProvider.OpenAI or ModelProvider.HuggingFace => string.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(chatConfiguration.ChatModelProvider),
            chatConfiguration.ChatModelProvider, null)
    };
    apiService.WithEnvironment("ConnectionStrings__embedding-model", connectionString);
    jupyter.WithEnvironment("ConnectionStrings__embedding-model", connectionString);
}

builder.Build().Run();
return;

string GetApiProviderKey(ModelProvider modelProvider)
{
    return modelProvider switch
    {
        ModelProvider.Ollama => "",
        ModelProvider.OllamaHost => "",
        ModelProvider.OpenAI => openAiKey.Resource.Value,
        ModelProvider.HuggingFace => hfApiKey.Resource.Value,
        _ => throw new ArgumentOutOfRangeException(nameof(modelProvider), modelProvider, null)
    };
}