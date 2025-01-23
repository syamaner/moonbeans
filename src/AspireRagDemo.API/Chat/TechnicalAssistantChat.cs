using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AspireRagDemo.API.Chat;

public class TechnicalAssistantChat(Kernel kernel) : ITechnicalAssistantChat
{
    private readonly PromptTemplateConfig _promptConfig = new()
    {
        Template = PromptTemplate,
        InputVariables =
        [
            new InputVariable { Name = "context" },
            new InputVariable { Name = "question" }
        ]
    };
    private const string PromptTemplate = @"You are a helpful AI assistant specialised in technical questions and good at utilising additional technical resources provided to you as additional context.
        Use the following context to answer the question. You pride yourself on bringing necessary references when needed.
        If you cannot find the answer in the context, say ""I cannot find the answer in the provided context.""

        Context:
        {{$context}}

        Question:
        {{$question}}

        Answer:";

    public async Task<string> GetResponseAsync(string context, string question)
    {
        var arguments = new KernelArguments
        {
            { "context", context },
            { "question", question }
        };

        var kernelFunction = kernel.CreateFunctionFromPrompt(_promptConfig);
        var result = await kernelFunction.InvokeAsync(kernel, arguments);
        return result.ToString();
    }
}
