using AspireRagDemo.ServiceDefaults;

namespace AspireRagDemo.AppHost.Extensions;

public static class OllamaExtensions
{
    public static IDistributedApplicationBuilder AddModelProvider(this IDistributedApplicationBuilder builder,
        ChatConfiguration chatConfiguration,
        out IResourceBuilder<OllamaModelResource>? chatModel,
        out IResourceBuilder<OllamaModelResource>? embeddingModel)
    {
        chatModel = null;
        embeddingModel = null;

        if(chatConfiguration.ChatModelProvider != ModelProvider.Ollama && chatConfiguration.EmbeddingModelProvider != ModelProvider.Ollama)
            return builder;
      
        // Following image is based off ollama 0.5.7 TAG and runs as non-root user.
        var ollama = builder.AddOllama(Constants.ConnectionStringNames.Ollama)
            // by default Ollama container runs as root so here is a custom image that runs as non-root user.
            // see ./Ollama/Dockerfile for more details.
            // build.sh script will build this image supprting multiple CPU architectures.
            .WithImage("syamaner/ollama-nonroot")
            .WithImageTag("0.5.7")
            .WithBindMount("./Ollama/data", "/home/ollama/.ollama")
            .WithLifetime(ContainerLifetime.Persistent);
     
       if (chatConfiguration.ChatModelProvider == ModelProvider.Ollama)
        {
            chatModel = ollama.AddModel(name: Constants.ConnectionStringNames.ChatModel, chatConfiguration.ChatModel);
        }

        if (chatConfiguration.EmbeddingModelProvider == ModelProvider.Ollama)
        {
            embeddingModel = ollama.AddModel(name: Constants.ConnectionStringNames.EmbeddingModel,
                chatConfiguration.EmbeddingModel);
        }

        return builder;
    }
}