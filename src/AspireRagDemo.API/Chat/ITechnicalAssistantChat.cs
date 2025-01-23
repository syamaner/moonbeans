namespace AspireRagDemo.API.Chat;

public interface ITechnicalAssistantChat
{
    Task<string> GetResponseAsync(string context, string question);
}