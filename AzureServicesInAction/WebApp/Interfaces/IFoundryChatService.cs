namespace WebApp.Interfaces
{
    public interface IFoundryChatService
    {
        Task<string> GetChatResponseAsync(string prompt);
    }
}
