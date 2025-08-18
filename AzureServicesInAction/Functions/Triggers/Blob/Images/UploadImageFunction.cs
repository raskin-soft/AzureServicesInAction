// Triggers/Blob/Images/UploadImageFunction.cs
using Functions.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


//public class UploadImageFunction(IImageService images, ILogger<UploadImageFunction> logger)
//{
//    [Function("ProcessProductImage")]
//    public async Task Run(
//        [BlobTrigger("uploads/{name}", Connection = "AzureWebJobsStorage")] Stream blob,
//        string name)
//    {
//        var result = await images.ProcessAsync(name, blob);
//        logger.LogInformation(
//            "Processed image {Name} ({Bytes} bytes) {WxH} -> thumb: {Thumb}",
//            result.BlobName, result.OriginalBytes, $"{result.Width}x{result.Height}", result.ThumbnailUri);
//    }
//}


public class UploadImageFunction()
{
    [Function("ProcessProductImage")]
    public void Run(
        [BlobTrigger("uploads/{name}", Connection = "AzureWebJobsStorage")] Stream blob,
        string name, FunctionContext context)
    {

        var logger = context.GetLogger("BlobTriggerFunction");

        logger.LogInformation("Raskin Insights > Blob uploaded from AzureServicesInAction Application: {Name}, size: {Size}", name, blob.Length);
    }
}