using Microsoft.SemanticKernel;

namespace AspireRagDemo.API.Chat;

public static class PromptConstants
{
    private const string RagPromptTemplate = """
                                             You are a helpful AI assistant specialised in technical questions and good at utilising additional technical resources provided to you as additional context.
                                                     Use the following context to answer the question. You pride yourself on bringing necessary references when needed.
                                                     If you cannot find the answer in the context, say "I cannot find the answer in the provided context."
                                             
                                                     Context:
                                                     {{$context}}
                                             
                                                     Question:
                                                     {{$question}}
                                             
                                                     Answer:
                                             """;

    private const string BasicChatPromptTemplate = """
                                                   You are a helpful AI assistant specialised in technical question.
                                                           You take pride on accuracy and you don't make things up.
                                                           If you are not sure about the answer, say "I cannot find the answer in the provided context."
                                                   
                                                           Question:
                                                           {{$question}}
                                                   
                                                           Answer:
                                                   """;
    
    /// <summary>
    /// To answer the question, the AI assistant will use the provided context.
    /// </summary>
    public static readonly PromptTemplateConfig RagPromptConfig = new()
    {
        Template = RagPromptTemplate,
        InputVariables =
        [
            new InputVariable { Name = "context" },
            new InputVariable { Name = "question" }
        ]
    };
    
    /// <summary>
    /// To answer the question, the AI assistant will not use any additional context.
    /// </summary>
    public static readonly PromptTemplateConfig BasicPromptConfig = new()
    {
        Template = BasicChatPromptTemplate,
        InputVariables =
        [
            new InputVariable { Name = "question" }
        ]
    };
}