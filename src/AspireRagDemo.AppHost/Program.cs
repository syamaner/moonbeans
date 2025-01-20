using AspireRagDemo.AppHost;
using AspireRagDemo.ServiceDefaults;
//ConnectionStrings__chat-model Endpoint=http://ollama:11434;Model=phi3.5
//ConnectionStrings__embedding-model Endpoint=http://ollama:11434;Model=mxbai-embed-large
//ConnectionStrings__qdrant Endpoint=http://qdrant:6334;Key=aMjJKx0t1a6E9hysaCacWz
//ConnectionStrings__qdrant_http Endpoint=http://qdrant:6333;Key=aMjJKx0t1a6E9hysaCacWz
//services__api-service__http__0 http://host.docker.internal:5026
var builder = DistributedApplication.CreateBuilder(args);
 
ChatConfiguration chatConfiguration = new(Environment.GetEnvironmentVariable("CHAT_MODEL") ?? "phi3.5", 
    Environment.GetEnvironmentVariable("EMBEDDING_MODEL") ?? "mxbai-embed-large",
    Enum.Parse<ModelProvider>(Environment.GetEnvironmentVariable("CHAT_MODEL_PROVIDER")?? "Ollama"),
    Enum.Parse<ModelProvider>(Environment.GetEnvironmentVariable("EMBEDDING_MODEL_PROVIDER")?? "Ollama"));

var openaiConnection = builder.AddConnectionString("openaiConnection");
var openAIKey = builder.AddParameter("OpenAIKey", secret: true);
var huggingFaceKey = builder.AddParameter("HuggingFaceKey", secret: true);

// When Jupyter server is launched, this is the secret to use when logging in to manage notebooks.
var jupyterLocalSecret = builder.AddParameter("JupyterSecret", secret: false);

Dictionary<string, int> applicationPorts = new()
{
    { Constants.ConnectionStringNames.ApiService, 5000 },
    { Constants.ConnectionStringNames.Ui, 8501 },
    { Constants.ConnectionStringNames.JupyterService, 8888 }
};

var chromaDb = builder.AddQdrant(Constants.ConnectionStringNames.Qdrant)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataBindMount(source: "./data/qdrant");
    
var ollama = builder.AddOllama(Constants.ConnectionStringNames.Ollama)
    .WithDataVolume();
var chatModel = ollama.AddModel(name: Constants.ConnectionStringNames.ChatModel,
    chatConfiguration.ChatModel);
var embeddingModel = ollama.AddModel(name:Constants.ConnectionStringNames.EmbeddingModel, 
    chatConfiguration.EmbeddingModel);

var apiService = builder.AddProject<Projects.AspireRagDemo_API>(Constants.ConnectionStringNames.ApiService) 
    .WithReference(chromaDb)
    .WithReference(chatModel)
    .WithReference(embeddingModel)
    //.WithEnvironment("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL","http://localhost:21268")
    //.WithEnvironment("OpenAIKey",openAIKey.Resource.Value)
    .WaitFor(chromaDb)
    .WaitFor(chatModel)
    .WaitFor(embeddingModel);

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
_ = builder
    .AddDockerfile(Constants.ConnectionStringNames.JupyterService, "./Jupyter")
    .WithBuildArg("PORT", applicationPorts[Constants.ConnectionStringNames.JupyterService])    
    .WithArgs($"--NotebookApp.token={jupyterLocalSecret.Resource.Value}")
    .WithBindMount("./Jupyter/Notebooks/","/home/jovyan/work")
    .WithHttpEndpoint(targetPort: applicationPorts[Constants.ConnectionStringNames.JupyterService], env: "PORT")
    .WithOtlpExporter()
    //http://host.docker.internal:21268    
    /*.WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT","http://host.docker.internal:16175")
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL","http/json")*/
    .WithEnvironment("OTEL_SERVICE_NAME","jupyterdemo")
    .WithEnvironment("OTEL_EXPORTER_OTLP_INSECURE","true")
    //.WithEnvironment("OTEL_TRACES_EXPORTER","console,otlp")
    .WithEnvironment("PYTHONUNBUFFERED", "0")
    .WithReference(chromaDb)
    .WithReference(apiService)
    .WithReference(embeddingModel)
    .WithReference(chatModel)
    .WaitFor(chromaDb)
    .WaitFor(apiService)
    .WaitFor(chatModel)
    .WaitFor(embeddingModel)
    .WithExternalHttpEndpoints();

builder.Build().Run();