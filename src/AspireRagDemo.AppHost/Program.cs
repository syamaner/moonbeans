using AspireRagDemo.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);
var ollamaHost = Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? "";
var vectorStoreVectorName = Environment.GetEnvironmentVariable("VECTOR_STORE_VECTOR_NAME") ?? "page_content_vector";
// this will be used for evaluating our performance in the evaluation notebook
// Ingestion and Query will use Ollama for both embeddings and generation.
var openAiKey = builder.AddParameter("OpenAIKey", secret: true);
 
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

var apiService = builder.AddProject<Projects.AspireRagDemo_API>(Constants.ConnectionStringNames.ApiService)    
    .WithEnvironment("ModelConfiguration__OpenAiApiKey",openAiKey.Resource.Value)
    .WithEnvironment("ModelConfiguration__VectorStoreVectorName",vectorStoreVectorName)
    .WithEnvironment("ModelConfiguration__OllamaUrl", ollamaHost)
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
    .WithReference(vectorStore)
    .WithReference(apiService)
    .WaitFor(vectorStore)
    .WaitFor(apiService)
    .WithExternalHttpEndpoints();

builder.Build().Run();