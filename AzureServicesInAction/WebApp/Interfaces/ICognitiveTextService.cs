namespace WebApp.Interfaces
{
    public interface ICognitiveTextService
    {
        string AnalyzeSentiment(string text);
        IEnumerable<string> ExtractKeyPhrases(string text);
    }
}
