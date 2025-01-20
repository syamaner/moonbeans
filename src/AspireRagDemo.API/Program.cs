using AspireRagDemo.ServiceDefaults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;

#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0001

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
var kernelBuilder = Kernel.CreateBuilder();

builder.Services.AddSingleton<QdrantClient>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString(Constants.ConnectionStringNames.Qdrant);
    var endpoint = connectionString?.Split(";")[0].Replace("Endpoint=", "");
    var key = connectionString?.Split(";")[1].Replace("Key=", "");

    return new QdrantClient(new Uri(endpoint ?? throw new InvalidOperationException("Qdrant endpoint cannot be null.")), key);
});
builder.Services.AddQdrantVectorStore();

builder.Services.AddKeyedSingleton<Kernel>(Constants.ConnectionStringNames.EmbeddingModel, (provider, key) =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString(key.ToString()!);
    var uri = new Uri(connectionString!.Split(";")[0].Replace("Endpoint=", ""));
    var httpClient = new HttpClient()
    {
        Timeout = TimeSpan.FromMinutes(10),
        BaseAddress = uri
    };
    var model = connectionString.Split(";")[1].Replace("Model=", "");
    var kernel = kernelBuilder.AddOllamaTextEmbeddingGeneration(model, httpClient, key.ToString())
        .Build();
    return kernel;
});
builder.Services.AddKeyedSingleton<Kernel>(Constants.ConnectionStringNames.ChatModel, (provider, key) =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString(key.ToString()!);
    var uri = new Uri(connectionString!.Split(";")[0].Replace("Endpoint=", ""));
    var model = connectionString.Split(";")[1].Replace("Model=", "");
    var httpClient = new HttpClient()
    {
        Timeout = TimeSpan.FromMinutes(10),
        BaseAddress = uri
    };
    var kernel = kernelBuilder.AddOllamaChatCompletion(model, httpClient, key.ToString())
        .Build();
    
    return kernel;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/embedding", async ([FromQuery] string input,
        [FromKeyedServices(key:Constants.ConnectionStringNames.EmbeddingModel)]
        Kernel kernel, [FromServices] QdrantClient qdrantClient) =>
    {
        string collection = "my_repo_collection";
       
        var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        return await embeddingGenerator.GenerateEmbeddingsAsync([input]);
 //       var result = await qdrantClient.SearchAsync(collection, vector[0], limit: 15);
  //      return result;
    })
    .WithName("GetEmbeddings");

app.MapGet("/rag", async ([FromQuery] string input,
        [FromKeyedServices(key:Constants.ConnectionStringNames.ChatModel)]Kernel kernel,        
        [FromKeyedServices(key:Constants.ConnectionStringNames.EmbeddingModel)] Kernel embeddingKernel,
        [FromServices] QdrantClient qdrantClient) =>
    {
        string collection = "my_repo_collection";
       
        var embeddingGenerator = embeddingKernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var vectors= await embeddingGenerator.GenerateEmbeddingsAsync([input]);
        var result = await qdrantClient.SearchAsync(collection, vectors[0], limit: 15);
        var x = result.ToString();
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        
        var chatOut  = await chat.GetChatMessageContentAsync(x);
        return chatOut;
    })
    .WithName("GetRag");

app.MapGet("/chat", async ([FromQuery] string input,
        [FromKeyedServices(key: Constants.ConnectionStringNames.ChatModel)] Kernel kernel) =>
    {
        if (input == "a") input = "In which city is the Eiffel Tower located?";
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        return await chat.GetChatMessageContentAsync(input);
    })
    .WithName("GetChat");
app.Run();