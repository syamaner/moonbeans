using System.Text;
using System.Text.Json;
using AspireRagDemo.API;
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
 
builder.AddVectorStore();
builder.AddSemanticKernelModels();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/embedding", async ([FromQuery] string input, [FromServices] Kernel kernel ) =>
    {      
        var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync([input]);
        return embeddings;
    })
    .WithName("GetEmbeddings");

app.MapGet("/vector-search", async ([FromQuery] string input,  
        [FromServices]Kernel kernel, 
        [FromServices] QdrantClient qdrantClient,
        [FromServices] IConfiguration configuration) =>
    {      
        var collection =  configuration["VectorStoreCollectionName"];
        var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var vectors= await embeddingGenerator.GenerateEmbeddingsAsync([input]);
        var result = await qdrantClient.SearchAsync(collection, vectors[0], limit: 5);
        return result;
    })
    .WithName("VectorSearch");

app.MapGet("/chat-with-context", async ([FromQuery] string input,  
        [FromServices]Kernel kernel, 
        [FromServices] QdrantClient qdrantClient,
        [FromServices] IConfiguration configuration, [FromServices]ITechnicalAssistantChat technicalAssistantChat) =>
    {
        var collection =  configuration["VectorStoreCollectionName"];
        var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var vectors= await embeddingGenerator.GenerateEmbeddingsAsync([input]);
        var result = await qdrantClient.SearchAsync(collection, vectors[0], limit: 5);
        
        var resultsTyped =  JsonSerializer.Deserialize<List<VectorQueryResult>>(result.ToString());
        var stbContext = new StringBuilder();
        foreach (var queryResult in resultsTyped)
        {
            stbContext.AppendLine(queryResult.Payload.PageContent.Value);
        }
        var chatOut  = await technicalAssistantChat.GetResponseAsync(stbContext.ToString(), input);
        return chatOut;
    })
    .WithName("RagChat");

app.MapGet("/chat", async ([FromQuery] string input, [FromServices]  Kernel kernel) =>
    {
        if (input == "a") input = "In which city is the Eiffel Tower located?";
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        return await chat.GetChatMessageContentAsync(input);
    })
    .WithName("RawChat");
app.Run();