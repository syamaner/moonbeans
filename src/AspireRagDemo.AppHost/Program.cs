using AspireRagDemo.AppHost;
using AspireRagDemo.AppHost.Extensions;
using AspireRagDemo.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);
ChatConfiguration chatConfiguration = new();

// When Jupyter server is launched, this is the secret to use when logging in to manage notebooks.
var jupyterLocalSecret = builder.AddParameter("JupyterSecret", secret: false);

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

// It is posible to use a model provider as following:
//  - Ollama using Aspire (might be good option if using an Nvidia Docker Compatible Docker host.
//  - Ollama on  host machine. Could be an option if using something like
//       a MacBook Pro with dedicated GPU where such features are not supported in Docker natively
//  - Also possible to use Open AI and Hugging Faces too.
// Downstream projects will resect the choices too so we only make the changes in app host configuration.
builder.AddModelProvider(chatConfiguration, 
    out var chatModel, 
    out var embeddingModel);

var apiService = builder.AddProject<Projects.AspireRagDemo_API>(Constants.ConnectionStringNames.ApiService)    
    .WithEnvironment("VectorStoreCollectionName", chatConfiguration.VectorStoreConnectionName)
    .WithEnvironment("EMBEDDING_MODEL",chatConfiguration.EmbeddingModel)
    .WithEnvironment("CHAT_MODEL",chatConfiguration.ChatModel)
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

// this will be used for evaluating our performance in the evaluation notebook
// Ingestion and Query will use Ollama for both embeddings and generation.
var openAiKey = builder.AddParameter("OpenAIKey", secret: true);
// For the ingestion pipeline and evaluation, we will be using Python and Jupyter.
var jupyter = builder
    .AddDockerfile(Constants.ConnectionStringNames.JupyterService, "./Jupyter")
    .WithBuildArg("PORT", applicationPorts[Constants.ConnectionStringNames.JupyterService])    
    .WithArgs($"--NotebookApp.token={jupyterLocalSecret.Resource.Value}")
    .WithBindMount("./Jupyter/Notebooks/","/home/jovyan/work")
    .WithHttpEndpoint(targetPort: applicationPorts[Constants.ConnectionStringNames.JupyterService], env: "PORT")
    .WithLifetime(ContainerLifetime.Session)
    .WithOtlpExporter()
    .WithEnvironment("OTEL_SERVICE_NAME","jupyterdemo")
    .WithEnvironment("OTEL_EXPORTER_OTLP_INSECURE","true")
    .WithEnvironment("PYTHONUNBUFFERED", "0")
    .WithEnvironment("OPENAI_KEY", openAiKey.Resource.Value)
    .WithEnvironment("VECTOR_STORE_COLLECTION_NAME", chatConfiguration.VectorStoreConnectionName)
    .WithEnvironment("EMBEDDING_MODEL",chatConfiguration.EmbeddingModel)
    .WithEnvironment("CHAT_MODEL",chatConfiguration.ChatModel)
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
    apiService.WithReference(chatModel)
        .WaitFor(chatModel);
    jupyter.WithReference(chatModel)
        .WaitFor(chatModel);
}
else
{
    apiService.WithEnvironment("ConnectionStrings__chat-model",
        chatConfiguration.ChatModelHostUri);
    jupyter.WithEnvironment("ConnectionStrings__chat-model", 
        chatConfiguration.ChatModelHostUri);
}

if(embeddingModel!=null)
{
    apiService.WithReference(embeddingModel)
        .WaitFor(embeddingModel);
    jupyter.WithReference(embeddingModel)
        .WaitFor(embeddingModel);
}
else
{
    apiService.WithEnvironment("ConnectionStrings__embedding-model",
        chatConfiguration.EmbeddingModelHostUri);
    jupyter.WithEnvironment("ConnectionStrings__embedding-model",
        chatConfiguration.EmbeddingModelHostUri);
}

builder.Build().Run();