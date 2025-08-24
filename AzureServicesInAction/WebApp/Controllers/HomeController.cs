using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Abstractions;
using System.Diagnostics;
using System.Reflection;
using WebApp.Interfaces;
using WebApp.Models;
using WebApp.Services.MetricsLogger;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;
        private readonly IMetricsLogger _metricsLogger;
        private readonly IChatService _chatService;

        public HomeController(ILogger<HomeController> logger, IConfiguration config, IMetricsLogger metricsLogger, IChatService chatService)
        {
            _logger = logger;
            _config = config;
            _metricsLogger = metricsLogger;
            _chatService = chatService;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> Upload()
        {
            await GetAllImages();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var containerName = "uploads";

            try
            {
                var stopwatch = Stopwatch.StartNew();

                _metricsLogger.TrackEvent("FileUploaded", new Dictionary<string, string>
                    {
                        { "FileName", file.FileName },
                        { "ContainerName", containerName },
                        { "BlobUrl", ViewBag.ImageUrl }
                    });

                var tenantId = _config["AzureAd:TenantId"];
                var clientId = _config["AzureAd:ClientId"];
                var clientSecret = _config["AzureAd:ClientSecret"];

                var keyVaultUrl = "https://raskinkeyvault.vault.azure.net/";

                var AzureADcredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                var client = new SecretClient(new Uri(keyVaultUrl), AzureADcredential);

                if (file != null && file.Length > 0)
                {
                    // ------------ > Use Azure Key Vault to get the connection string
                    KeyVaultSecret secret = await client.GetSecretAsync("BlobConnection");
                    string connectionString = secret.Value;

                    var containerClient = new BlobContainerClient(connectionString, containerName);
                    await containerClient.CreateIfNotExistsAsync();

                    var blobClient = containerClient.GetBlobClient(file.FileName);
                    using var stream = file.OpenReadStream();
                    await blobClient.UploadAsync(stream, overwrite: true);



                    // ------------ > Use appsettings.json > Environment Variables to get the connection string
                    //var connectionString = _config["environmentVariables:BlobConnection"];

                    //var blobServiceClient = new BlobServiceClient(connectionString);
                    //var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                    //await containerClient.CreateIfNotExistsAsync();

                    //var blobClient = containerClient.GetBlobClient(file.FileName);
                    //using var stream = file.OpenReadStream();
                    //await blobClient.UploadAsync(stream, overwrite: true);



                    // ------------ > Generate SAS URL (valid for 1 hour)
                    var accountName = GetValueFromConnectionString(connectionString, "AccountName");
                    var accountKey = GetValueFromConnectionString(connectionString, "AccountKey");

                    var credential = new StorageSharedKeyCredential(accountName, accountKey);

                    var sasBuilder = new BlobSasBuilder
                    {
                        BlobContainerName = containerName,
                        BlobName = file.FileName,
                        Resource = "b",
                        ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                    };
                    sasBuilder.SetPermissions(BlobSasPermissions.Read);

                    var sasUri = blobClient.GenerateSasUri(sasBuilder);

                    ViewBag.Message = "File uploaded to Blob Storage!";
                    ViewBag.ImageUrl = sasUri.ToString();
                }

                stopwatch.Stop();
                _metricsLogger.TrackPerformance("FileUploadTime", stopwatch.ElapsedMilliseconds);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                ViewBag.Message = "Error uploading file: " + ex.Message;
            }

            return View();
        }

        private string GetValueFromConnectionString(string connectionString, string key)
        {
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
                {
                    return part.Substring(key.Length + 1);
                }
            }
            return null;
        }

        public async Task<IActionResult> GetAllImages()
        {
            //var connectionString = _config["environmentVariables:BlobConnection"];

            var stopwatch = Stopwatch.StartNew();

            var tenantId = _config["AzureAd:TenantId"];
            var clientId = _config["AzureAd:ClientId"];
            var clientSecret = _config["AzureAd:ClientSecret"];

            var keyVaultUrl = "https://raskinkeyvault.vault.azure.net/";

            try
            {
                var AzureADcredential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                var client = new SecretClient(new Uri(keyVaultUrl), AzureADcredential);

                KeyVaultSecret secret = await client.GetSecretAsync("BlobConnection");
                string connectionString = secret.Value;
                var containerName = "uploads";

                var blobServiceClient = new BlobServiceClient(connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                var imageUrls = new List<string>();

                await foreach (BlobItem blobItem in containerClient.GetBlobsAsync())
                {
                    var blobClient = containerClient.GetBlobClient(blobItem.Name);
                    var imageUrl = blobClient.Uri.ToString();

                    if (blobItem.Properties.ContentType?.StartsWith("image/") == true ||
                        blobItem.Name.EndsWith(".jpg") || blobItem.Name.EndsWith(".png") || blobItem.Name.EndsWith(".jpeg"))
                    {
                        var sasBuilder = new BlobSasBuilder
                        {
                            BlobContainerName = containerName,
                            BlobName = blobItem.Name,
                            Resource = "b",
                            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                        };
                        sasBuilder.SetPermissions(BlobSasPermissions.Read);

                        var sasUri = blobClient.GenerateSasUri(sasBuilder);
                        imageUrls.Add(sasUri.ToString());
                        //imageUrls.Add(imageUrl);
                    }
                }

                stopwatch.Stop();
                _metricsLogger.TrackPerformance("AllImagesLoadTime", stopwatch.ElapsedMilliseconds);

                return View(imageUrls);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving images");
                ViewBag.Message = "Error retrieving images: " + ex.Message;
                return View(new List<string>());
            }
        }


        [HttpGet]
        public async Task<IActionResult> ChatWithAI()
        {
            await AIAnalysis();
            return View(new ChatViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> ChatWithAI(ChatViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.UserPrompt))
            {
                model.Response = await _chatService.GetResponseAsync(model.UserPrompt);
            }

            return View(model);
        }

        public async Task<IActionResult> AIAnalysis()
        {

            //// --------- Summarization
            //var prompt = """
            //Summarize the following customer feedback in 3 bullet points:

            //"I love the product quality, but delivery was slow. Support team was helpful. I’ll buy again."
            //""";


            //            // --------- Sentiment Analysis
            //            var prompt = """
            //Analyze the sentiment of this review. Is it positive, negative, or mixed?

            //"I love the product quality, but delivery was slow. Support team was helpful."
            //""";


            //            // --------- Recommendation
            //            var prompt = """
            //Based on this user's purchase history, recommend 3 products:

            //- Bought: WidgetA, WidgetB
            //- Browsed: WidgetC, WidgetD
            //""";


            // --------- Predictive Reasoning(Not true forecasting)
            var prompt = """
                            Based on the following sales data, what trend do you observe?

                            Date       | Product | Sales
                            2025-08-01 | WidgetA | 120
                            2025-08-02 | WidgetA | 150
                            2025-08-03 | WidgetA | 180
                        """;


            ViewBag.AIAnalysis = await _chatService.GetResponseAsync(prompt);

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
