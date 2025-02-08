using AspireRagDemo.API;
using AspireRagDemo.API.Chat;
using AspireRagDemo.API.Extensions;
using AspireRagDemo.API.Models;
using AspireRagDemo.ServiceDefaults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;

#pragma warning disable SKEXP0070
#pragma warning disable SKEXP0001

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddSemanticKernelModels();
builder.Services.Configure<ModelConfiguration>(builder.Configuration.GetSection("ModelConfiguration"));

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/vector-search", async ([FromQuery] string query,
        [FromServices] Kernel kernel,
        [FromServices] QdrantClient qdrantClient,
        [FromServices] IOptions<ModelConfiguration> configuration) =>
    {
        var collectionName = configuration.Value.VectorStoreCollectionName ?? throw new InvalidOperationException(
            $"Model Configuration {nameof(configuration.Value.VectorStoreCollectionName)} cannot be null.");
        
        var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var vectors = await embeddingGenerator.GenerateEmbeddingsAsync([query]);
        var result = await qdrantClient.SearchAsync(collectionName, vectors[0], limit: 5);
        return result;
    })
    .WithName("VectorSearch");

app.MapGet("/chat-with-context", async ([FromQuery] string query,
        [FromServices] IChatClient technicalAssistantChat,
        [FromServices] IOptions<ModelConfiguration> configuration) =>
    {
        //Can you please explain why Should I learn .Net Aspire if I already know Docker Compose?
        var answer = await technicalAssistantChat.AnswerQuestion(query, true);
        return new ChatResponse(answer, query, configuration.Value.EmbeddingModel, configuration.Value.ChatModel);
    })
    .WithName("RagChat");

app.MapGet("/chat", async ([FromQuery] string query, [FromServices] IChatClient technicalAssistantChat,
        [FromServices] IOptions<ModelConfiguration> configuration) =>
    {
        var answer = await technicalAssistantChat.AnswerQuestion(query, false);
        return new ChatResponse(answer, query, configuration.Value.EmbeddingModel, configuration.Value.ChatModel);
    })
    .WithName("BasicChat");

app.Run();