using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AspireRagDemo.API;

public interface ITechnicalAssistantChat
{
    Task<string> GetResponseAsync(string context, string question);
}
public class TechnicalAssistantChat : ITechnicalAssistantChat
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletion;
    private readonly string _systemPrompt;
    
    public TechnicalAssistantChat(Kernel kernel)
    {
        _kernel = kernel;
        _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        _systemPrompt = @"You are a helpful AI assistant specialised in technical questions and good at utilising additional technical resources provided to you as additional context.
Use the following context to answer the question.
If you cannot find the answer in the context, say ""I cannot find the answer in the provided context.""

Context:
{{$context}}

Question:
{{$question}}

Answer:";
    }

    public async Task<string> GetResponseAsync(string context, string question)
    {
        // Create chat history
        var chatHistory = new ChatHistory();
        
        // Add system message
        chatHistory.AddSystemMessage(_systemPrompt);
        
        // Create the prompt template config
        var promptConfig = new PromptTemplateConfig
        {
            Template = _systemPrompt,//"context", "question"
            InputVariables = new List<InputVariable>() { new InputVariable(){Name = "context"},
                new InputVariable(){Name = "question"} }
        };

        // Create the kernel function from the prompt template
        var kernelFunction = _kernel.CreateFunctionFromPrompt(promptConfig);

        // Set the context variables
        var arguments = new KernelArguments
        {
            { "context", context },
            { "question", question }
        };

        // Get the response
        var result = await kernelFunction.InvokeAsync(_kernel, arguments);
        return result.ToString();
    }
}
