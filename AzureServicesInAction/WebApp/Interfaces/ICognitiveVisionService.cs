namespace WebApp.Interfaces
{
    public interface ICognitiveVisionService
    {
        Task<string> ExtractTextFromImageAsync(Stream imageStream);
    }
}
