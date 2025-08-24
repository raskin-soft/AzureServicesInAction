namespace WebApp.Interfaces
{
    public interface IChatService
    {
        Task<string> GetResponseAsync(string prompt);
    }
}
