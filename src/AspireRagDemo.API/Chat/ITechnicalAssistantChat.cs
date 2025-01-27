namespace AspireRagDemo.API.Chat;

public interface ITechnicalAssistantChat
{
    Task<string> AnswerQuestion(string question, bool useAdditionalContext);
}