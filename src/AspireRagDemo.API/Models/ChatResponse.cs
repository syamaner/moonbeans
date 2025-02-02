namespace AspireRagDemo.API.Models;

record ChatResponse(string Answer, string Query, string EmbeddingModel, string ChatModel);