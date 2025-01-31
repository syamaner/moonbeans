using System.Text;
using AspireRagDemo.API.Models;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
#pragma warning disable SKEXP0001

namespace AspireRagDemo.API.Chat;

public class TechnicalAssistantChat(
    Kernel kernel,
    IVectorStore vectorStore,
    IConfiguration configuration,
    ILogger<TechnicalAssistantChat> logger) : ITechnicalAssistantChat
{
    private const short TopSearchResults = 20;

    private readonly ITextEmbeddingGenerationService _embeddingGenerator =
        kernel.GetRequiredService<ITextEmbeddingGenerationService>();

    private readonly IVectorStoreRecordCollection<ulong, FaqRecord> _faqCollection =
        vectorStore.GetCollection<ulong, FaqRecord>(configuration["VectorStoreCollectionName"]
                                                    ?? throw new InvalidOperationException(
                                                        $"Configuration variable VectorStoreCollectionName can't be empty."));

    /// <summary>
    /// Answer the question based on the provided question and optionally using additional context.
    /// </summary>
    /// <param name="question"></param>
    /// <param name="useAdditionalContext">If true, this method will act as a basic RAG query.
    ///  Otherwise, the method will rely on the model without and extra information.
    /// </param>
    /// <returns></returns>
    public async Task<string> AnswerQuestion(string question, bool useAdditionalContext)
    {
        try
        {
            if (!useAdditionalContext) return await AnswerWithoutAdditionalContext(question);

            var context = await GetContextFromVectorStore(question);
            return await AnswerWithAdditionalContext(context, question);
        }
        catch (Exception e)
        {
            logger.LogError(e,
                "Error while answering the question. Additional context required? : {useAdditionalContext}",
                useAdditionalContext);
            return "I am sorry, I am unable to answer the question at the moment.";
        }
    }

    private async Task<string> AnswerWithoutAdditionalContext(string question)
    {
        try
        {
            var arguments = new KernelArguments
            {
                { "question", question }
            };

            var kernelFunction = kernel.CreateFunctionFromPrompt(PromptConstants.BasicPromptConfig);
            var result = await kernelFunction.InvokeAsync(kernel, arguments);
            return result.ToString();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while answering the question without additional context.");
            return "I am sorry, I am unable to answer the question at the moment.";
        }
    }

    private async Task<string> AnswerWithAdditionalContext(string context, string question)
    {
        var arguments = new KernelArguments
        {
            { "context", context },
            { "question", question }
        };

        var kernelFunction = kernel.CreateFunctionFromPrompt(PromptConstants.RagPromptConfig);
        var result = await kernelFunction.InvokeAsync(kernel, arguments);
        return result.ToString();
    }

    /// <summary>
    /// Get context from the vector store based on the question.
    ///  This method uses the vector store to search for the most relevant context based on the question:
    ///      1. Retrieve the embeddings using the embedding model
    ///      2. Search the vector store for the most relevant context based on the embeddings.
    ///      3. Return the context as a string.
    /// </summary>
    /// <param name="question"></param>
    /// <returns>Vector Search Results.</returns>
    private async Task<string> GetContextFromVectorStore(string question)
    {
        var questionVectors =
            await _embeddingGenerator.GenerateEmbeddingsAsync([question]);

        var stbContext = new StringBuilder();

        var searchResults = await _faqCollection.VectorizedSearchAsync(questionVectors[0],
            new VectorSearchOptions() { Top = TopSearchResults });

        await foreach (var item in searchResults.Results)
        {
            stbContext.AppendLine(item.Record.Content);
        }

        return stbContext.ToString();
    }
}