using System.Diagnostics;
using AspireRagDemo.API;
using AspireRagDemo.API.Chat;
using AspireRagDemo.API.Extensions;
using AspireRagDemo.API.Ingestion;
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

builder.Services.AddSingleton<IngestionPipeline>();
builder.Services.AddSingleton<IChunker,GitIngestChunker>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.MapGet("/chat-with-context", async ([FromQuery] string query, [FromQuery] string embeddingModel, [FromQuery] string chatModel,
        [FromServices] IChatClient technicalAssistantChat,
        [FromServices] IOptions<ModelConfiguration> configuration) =>
    {
        //Can you please explain why Should I learn .Net Aspire if I already know Docker Compose?
        var answer = await technicalAssistantChat.AnswerQuestion(query, true,embeddingModel);
        return new ChatResponse(answer, query, embeddingModel, chatModel);
    })
    .WithName("RagChat");

app.MapGet("/chat", async ([FromQuery] string query,[FromQuery] string embeddingModel, 
        [FromQuery] string chatModel, [FromServices] IChatClient technicalAssistantChat,
        [FromServices] IOptions<ModelConfiguration> configuration) =>
    {
        var answer = await technicalAssistantChat.AnswerQuestion(query, false, embeddingModel);
        return new ChatResponse(answer, query, embeddingModel, chatModel);
    })
    .WithName("BasicChat");
ActivitySource source = new ActivitySource("IngestionApi", "1.0.0");

app.MapGet("/ingest", async ([FromQuery] string fileName,[FromQuery] string? embeddingModel, 
        [FromServices] IngestionPipeline ingestionPipeline,   [FromServices] IOptions<ModelConfiguration> configuration) =>
    {
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "dotnet-docs-aspire.txt";
        if(!string.IsNullOrWhiteSpace(embeddingModel))
        {
            await ingestionPipeline.IngestDataAsync(fileName, DocumentType.GitIngest, embeddingModel);            
        }
        else
        {
            using var activity = source.StartActivity("Bulk ingest");
            foreach (var benchmarkConfiguration in configuration.Value.BenchmarkConfigurations)
            {
                using var activity1 = source.StartActivity(name:"Ingest for embedding model", kind:ActivityKind.Internal, 
                  tags:  [new KeyValuePair<string, object?>("EmbeddingModel", benchmarkConfiguration.EmbeddingModel)]);
                await ingestionPipeline.IngestDataAsync(fileName, DocumentType.GitIngest,
                    benchmarkConfiguration.EmbeddingModel);
            }
        }
        return true;
    })
    .WithName("Ingest");

app.Run();