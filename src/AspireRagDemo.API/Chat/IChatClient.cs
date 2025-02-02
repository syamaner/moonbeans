namespace AspireRagDemo.API.Chat;

public interface IChatClient
{
    Task<string> AnswerQuestion(string question, bool useAdditionalContext);
}