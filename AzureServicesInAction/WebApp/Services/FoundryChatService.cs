using System.Text.Json;
using WebApp.Interfaces;

namespace WebApp.Services
{
    public class FoundryChatService : IFoundryChatService
    {
        private readonly HttpClient _client;
        private readonly string _deploymentName;
        private readonly string _apiVersion = "2024-02-15-preview";

        public FoundryChatService(IHttpClientFactory factory, IConfiguration config)
        {
            _client = factory.CreateClient("AzureFoundry");
            _deploymentName = config["AzureFoundry:DeploymentName"];
        }

        public async Task<string> GetChatResponseAsync(string prompt)
        {
            var payload = new
            {
                messages = new[]
                {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = prompt }
            },
                temperature = 0.7
            };

            var response = await _client.PostAsJsonAsync(
                $"/openai/deployments/{_deploymentName}/chat/completions?api-version={_apiVersion}",
                payload);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
    }
}
