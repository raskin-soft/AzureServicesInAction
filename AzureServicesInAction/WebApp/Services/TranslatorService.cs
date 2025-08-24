using Azure;
using Azure.AI.Translation.Text;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebApp.Interfaces;

namespace WebApp.Services
{
    public class TranslatorService : ITranslatorService
    {
        private readonly string _endpoint;
        //private readonly string _region;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public TranslatorService(IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _endpoint = config["AzureTranslator:Endpoint"] ?? throw new ArgumentNullException("Endpoint missing");
            //_region = config["AzureTranslator:Region"] ?? throw new ArgumentNullException("Region missing");
            _apiKey = config["AzureTranslator:ApiKey"] ?? throw new ArgumentNullException("ApiKey missing");

            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<string> TranslateTextAsync(string inputText, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(inputText)) return string.Empty;

            //string route = $"/translate?api-version=3.0&to={targetLanguage}";
            //string requestUri = _endpoint.TrimEnd('/') + route;

            string route = $"/translator/text/v3.0/translate?api-version=3.0&to={targetLanguage}";
            string requestUri = _endpoint.TrimEnd('/') + route;


            var requestBody = new object[] { new { Text = inputText } };
            string requestJson = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
            //request.Headers.Add("Ocp-Apim-Subscription-Region", _region);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string resultJson = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(resultJson);
            return doc.RootElement[0].GetProperty("translations")[0].GetProperty("text").GetString()!;
        }
    }
}
