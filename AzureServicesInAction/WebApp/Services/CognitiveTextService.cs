using Azure;
using Azure.AI.TextAnalytics;
using WebApp.Interfaces;

namespace WebApp.Services
{
    public class CognitiveTextService : ICognitiveTextService
    {
        private readonly TextAnalyticsClient _client;

        public CognitiveTextService(IConfiguration config)
        {
            var endpoint = new Uri(config["AzureCognitive:Endpoint"]);
            var key = new AzureKeyCredential(config["AzureCognitive:ApiKey"]);
            _client = new TextAnalyticsClient(endpoint, key);
        }

        public string AnalyzeSentiment(string text)
        {
            var result = _client.AnalyzeSentiment(text);
            return $"Sentiment: {result.Value.Sentiment}, Positive: {result.Value.ConfidenceScores.Positive:P0}";
        }

        public IEnumerable<string> ExtractKeyPhrases(string text)
        {
            var result = _client.ExtractKeyPhrases(text);
            return result.Value;
        }
    }
}
