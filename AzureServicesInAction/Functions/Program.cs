using Azure.Storage.Blobs;
using Functions.Options;
using Functions.Services;
using Functions.Services.Implementations;
using Functions.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication() // or .ConfigureFunctionsWorkerDefaults() if you don't need ASP.NET Core features
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<StorageOptions>(ctx.Configuration.GetSection("Storage"));

        // OPTION A: simplest
        services.AddSingleton(sp =>
        {
            var cs = ctx.Configuration["AzureWebJobsStorage"];
            return new BlobServiceClient(cs);
        });

        // OPTION B: or, use AddAzureClients (connection string)
        // services.AddAzureClients(builder =>
        // {
        //     builder.AddBlobServiceClient(ctx.Configuration["AzureWebJobsStorage"]);
        // });


        services.AddSingleton<IImageService, ImageService>();
    })
    .Build();

host.Run();