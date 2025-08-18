using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Functions.Models;
using Functions.Options;
using Functions.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
// Services/Implementations/ImageService.cs
using System;
using System.Buffers;
using System.Collections.Generic;
//using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace Functions.Services.Implementations
{
    public sealed class ImageService : IImageService
    {
        private readonly BlobServiceClient _blobService;
        private readonly ILogger<ImageService> _logger;
        private readonly StorageOptions _storage;

        public ImageService(
            BlobServiceClient blobService,
            IOptions<StorageOptions> storageOptions,
            ILogger<ImageService> logger)
        {
            _blobService = blobService;
            _logger = logger;
            _storage = storageOptions.Value;
        }

        public async Task<ImageProcessResult> ProcessAsync(string blobName, Stream blobStream, ImageProcessOptions? options = null)
        {
            options ??= new ImageProcessOptions();

            // Copy to memory because BlobTrigger stream may be non-seekable
            //await using var mem = await CopyToMemoryAsync(blobStream);
            //mem.Position = 0;

            using var mem = new MemoryStream();
            await blobStream.CopyToAsync(mem);
            mem.Position = 0;

            // Detect content type and validate
            var contentType = DetectContentType(mem);
            if (!IsSupportedImage(contentType))
                throw new InvalidOperationException($"Unsupported content type: {contentType}");

            mem.Position = 0;

            // Load image to read dimensions and EXIF
            using Image image = await Image.LoadAsync(mem.ToString());
            var (w, h) = (image.Width, image.Height);

            var exifData = ExtractBasicExif(image.Metadata.ExifProfile);

            // Reset for uploads
            mem.Position = 0;

            // Save original into processed container (optional copy/normalize step)
            var processedUri = await UploadAsync(
                container: _storage.ProcessedContainer,
                name: blobName,
                content: mem,
                contentType: contentType,
                overwrite: options.Overwrite,
                metadata: exifData);

            // Generate thumbnail
            //var thumbName = AppendSuffix(blobName, "_thumb", format.FileExtensions.Count > 0 ? format.FileExtensions[0] : null);
            var thumbName = blobName + "_thumb";
            string ? thumbnailUri = null;

            using (var thumb = image.Clone(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(options.ThumbnailMaxWidth, options.ThumbnailMaxHeight),
                Sampler = KnownResamplers.Lanczos3
            })))
            //await using (var thumbStream = new MemoryStream())
            //{
            //    await thumb.SaveAsync(thumbStream, format);
            //    thumbStream.Position = 0;

            //    thumbnailUri = await UploadAsync(
            //        container: _storage.ThumbnailsContainer,
            //        name: thumbName,
            //        content: thumbStream,
            //        contentType: contentType,
            //        overwrite: options.Overwrite,
            //        metadata: new Dictionary<string, string>
            //        {
            //            ["source"] = blobName,
            //            ["width"] = thumb.Width.ToString(),
            //            ["height"] = thumb.Height.ToString()
            //        });
            //}

            //return new ImageProcessResult
            //{
            //    BlobName = blobName,
            //    OriginalBytes = mem.Length,
            //    Width = w,
            //    Height = h,
            //    ContentType = contentType,
            //    OriginalUri = processedUri,
            //    ThumbnailUri = thumbnailUri,
            //    Metadata = exifData
            //};

            return new ImageProcessResult
            {
                BlobName = blobName,
                OriginalBytes = mem.Length,
                Width = w,
                Height = h,
                ContentType = contentType,
                OriginalUri = processedUri,
                ThumbnailUri = "",
                Metadata = exifData
            };
        }

        private async Task<string> UploadAsync(string container, string name, Stream content, string contentType, bool overwrite, IDictionary<string, string>? metadata)
        {
            var containerClient = _blobService.GetBlobContainerClient(container);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

            var blobClient = containerClient.GetBlobClient(name);
            var headers = new BlobHttpHeaders { ContentType = contentType };

            content.Position = 0;
            if (overwrite)
            {
                await blobClient.UploadAsync(content, new BlobUploadOptions
                {
                    HttpHeaders = headers,
                    Metadata = metadata != null ? new Dictionary<string, string>(metadata) : null
                }, default);
            }
            else
            {
                try
                {
                    await blobClient.UploadAsync(content, new BlobUploadOptions
                    {
                        HttpHeaders = headers,
                        Metadata = metadata != null ? new Dictionary<string, string>(metadata) : null,
                        Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
                    }, default);
                }
                catch (RequestFailedException ex) when (ex.Status == 412)
                {
                    _logger.LogInformation("Blob already exists and overwrite is false: {Name}", name);
                }
            }

            return blobClient.Uri.ToString();
        }

        private static async Task<MemoryStream> CopyToMemoryAsync(Stream input)
        {
            var ms = new MemoryStream();
            await input.CopyToAsync(ms);
            return ms;
        }

        private static string DetectContentType(Stream s)
        {
            // Minimal magic-byte detection
            s.Position = 0;
            var buffer = ArrayPool<byte>.Shared.Rent(12);
            try
            {
                var read = s.Read(buffer, 0, 12);
                s.Position = 0;

                // JPEG
                if (read >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF)
                    return "image/jpeg";
                // PNG
                if (read >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
                    return "image/png";
                // GIF
                if (read >= 6 && buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46)
                    return "image/gif";
                // WebP
                if (read >= 12 && buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
                    buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                    return "image/webp";

                return "application/octet-stream";
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static bool IsSupportedImage(string contentType)
            => contentType is "image/jpeg" or "image/png" or "image/gif" or "image/webp";

        private static Dictionary<string, string> ExtractBasicExif(ExifProfile? exif)
        {
            var result = new Dictionary<string, string>();
            if (exif == null) return result;

            void Add<TValueType>(ExifTag<TValueType> tag, string key)
            {
                var exifValue = exif.Values.FirstOrDefault(v => v.Tag.Equals(tag)) as IExifValue<TValueType>;
                var value = exifValue?.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(value)) result[key] = value!;
            }

            Add(ExifTag<string>.Make, "exif_make");
            Add(ExifTag<string>.Model, "exif_model");
            Add(ExifTag<string>.DateTimeOriginal, "exif_date");
            Add(ExifTag<string>.LensModel, "exif_lens");
            return result;
        }

        private static string AppendSuffix(string name, string suffix, string? extFromFormat)
        {
            var filename = Path.GetFileNameWithoutExtension(name);
            var ext = Path.GetExtension(name);
            if (string.IsNullOrEmpty(ext) && !string.IsNullOrEmpty(extFromFormat))
                ext = "." + extFromFormat;
            return $"{filename}{suffix}{ext}";
        }
    }
}
