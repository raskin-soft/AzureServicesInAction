// Services/Interfaces/IImageService.cs
using Functions.Models;
using System.IO;
using System.Threading.Tasks;

namespace Functions.Services.Interfaces
{
    public interface IImageService
    {
        Task<ImageProcessResult> ProcessAsync(string blobName, Stream blobStream, ImageProcessOptions? options = null);
    }
}
