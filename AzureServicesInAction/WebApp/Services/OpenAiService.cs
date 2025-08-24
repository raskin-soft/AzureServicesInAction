using System.Text.Json;
using WebApp.Interfaces;

namespace WebApp.Services
{
    public class OpenAiService : IChatService
    {
        private readonly HttpClient _client;
        private readonly string _deploymentName;
        private readonly string _apiVersion = "2023-03-15-preview";

        public OpenAiService(IHttpClientFactory factory, IConfiguration config)
        {
            _client = factory.CreateClient("AzureOpenAI");
            _deploymentName = config["AzureOpenAI:DeploymentName"]
                              ?? throw new ArgumentNullException("DeploymentName not configured");
        }

        public async Task<string> GetResponseAsync(string prompt)
        {
            var payload = new
            {
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 500,
                temperature = 0.7
            };

            var endpoint = $"/openai/deployments/{_deploymentName}/chat/completions?api-version={_apiVersion}";
            var response = await _client.PostAsJsonAsync(endpoint, payload);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Azure OpenAI failed: {response.StatusCode} - {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            return result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
    }
}
