using AspireRagDemo.AppHost;
using AspireRagDemo.ServiceDefaults;

var builder = DistributedApplication.CreateBuilder(args);
 
ChatConfiguration chatConfiguration = new(Environment.GetEnvironmentVariable("CHAT_MODEL") ?? "phi3.5", 
    Environment.GetEnvironmentVariable("EMBEDDING_MODEL") ?? "mxbai-embed-large",
    Enum.Parse<ModelProvider>(Environment.GetEnvironmentVariable("CHAT_MODEL_PROVIDER")?? "Ollama"),
    Enum.Parse<ModelProvider>(Environment.GetEnvironmentVariable("EMBEDDING_MODEL_PROVIDER")?? "Ollama"));

//this will be used for evaluating our performance in the evaluation notebook
var openAiKey = builder.AddParameter("OpenAIKey", secret: true);
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
    .WithLifetime(ContainerLifetime.Session)
    .WithDataBindMount(source: "./data/qdrant");

// Following image is based off ollama 0.5.7 TAG and runs as non-root user.
var ollama = builder.AddOllama(Constants.ConnectionStringNames.Ollama)
    .WithImage("syamaner/ollama-nonroot")
    .WithLifetime(ContainerLifetime.Session)
    .WithImageTag("1")
    .WithBindMount("./Ollama/data","/home/ollama/.ollama");
   // .WithDataVolume();

// Models are driven by environment variables in launchSettings.json
var chatModel = ollama.AddModel(name: Constants.ConnectionStringNames.ChatModel,
    chatConfiguration.ChatModel);
var embeddingModel = ollama.AddModel(name:Constants.ConnectionStringNames.EmbeddingModel, 
    chatConfiguration.EmbeddingModel);

var apiService = builder.AddProject<Projects.AspireRagDemo_API>(Constants.ConnectionStringNames.ApiService) 
    .WithReference(vectorStore)
    .WithReference(chatModel)
    .WithReference(embeddingModel)
    .WaitFor(vectorStore)
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
    .WithLifetime(ContainerLifetime.Session)
    .WithOtlpExporter()
    .WithEnvironment("OTEL_SERVICE_NAME","jupyterdemo")
    .WithEnvironment("OTEL_EXPORTER_OTLP_INSECURE","true")
    .WithEnvironment("PYTHONUNBUFFERED", "0")
    .WithEnvironment("OPENAI_KEY", openAiKey.Resource.Value)
    .WithReference(vectorStore)
    .WithReference(apiService)
    .WithReference(embeddingModel)
    .WithReference(chatModel)
    .WaitFor(vectorStore)
    .WaitFor(apiService)
    .WaitFor(chatModel)
    .WaitFor(embeddingModel)
    .WithExternalHttpEndpoints();

builder.Build().Run();