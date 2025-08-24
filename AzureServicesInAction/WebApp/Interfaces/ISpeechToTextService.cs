namespace WebApp.Interfaces
{
    public interface ISpeechToTextService
    {
        Task<string> TranscribeFromMicrophoneAsync();
        Task<string> TranscribeFromFileAsync(string filePath);
    }
}
