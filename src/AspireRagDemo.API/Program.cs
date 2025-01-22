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

app.MapGet("/embedding", async ([FromQuery] string query, [FromServices] Kernel kernel ) =>
    {      
        var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync([query]);
        return embeddings;
    })
    .WithName("GetEmbeddings");

app.MapGet("/vector-search", async ([FromQuery] string query,  
        [FromServices]Kernel kernel, 
        [FromServices] QdrantClient qdrantClient,
        [FromServices] IConfiguration configuration) =>
    {      
        var collection =  configuration["VectorStoreCollectionName"];
        var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var vectors= await embeddingGenerator.GenerateEmbeddingsAsync([query]);
        var result = await qdrantClient.SearchAsync(collection, vectors[0], limit: 5);
        return result;
    })
    .WithName("VectorSearch");

app.MapGet("/chat-with-context", async ([FromQuery] string query,  
        [FromServices]Kernel kernel, 
        [FromServices] QdrantClient qdrantClient,
        [FromServices] IConfiguration configuration, [FromServices]ITechnicalAssistantChat technicalAssistantChat) =>
    {
        var collection =  configuration["VectorStoreCollectionName"];
        var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var vectors= await embeddingGenerator.GenerateEmbeddingsAsync([query]);
        var result = await qdrantClient.SearchAsync(collection, vectors[0], limit: 10);
        
        var resultsTyped =  JsonSerializer.Deserialize<List<VectorQueryResult>>(result.ToString());
        var stbContext = new StringBuilder();
        foreach (var queryResult in resultsTyped)
        {
            stbContext.AppendLine(queryResult.Payload.PageContent.Value);
        }
        var chatOut  = await technicalAssistantChat.GetResponseAsync(stbContext.ToString(), query);
        return  new ChatResponse(chatOut, query);
    })
    .WithName("RagChat");

app.MapGet("/chat", async ([FromQuery] string query, [FromServices]  Kernel kernel) =>
    {
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        ChatHistory history = new ChatHistory();
        history.AddSystemMessage("You are a helpful AI assistant specialised in technical questions. \nIf you are unsure about the answer, say \"\"I cannot find the answer in the provided context.");
        history.AddUserMessage(query);    
        return await chat.GetChatMessageContentAsync(history);
    })
    .WithName("RawChat");
app.Run();