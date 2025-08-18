// Models/ImageProcessResult.cs
using System.Collections.Generic;

namespace Functions.Models
{
    public sealed class ImageProcessResult
    {
        public string BlobName { get; init; } = default!;
        public long OriginalBytes { get; init; }
        public int? Width { get; init; }
        public int? Height { get; init; }
        public string ContentType { get; init; } = "application/octet-stream";
        public string OriginalUri { get; init; } = default!;
        public string? ThumbnailUri { get; init; }
        public Dictionary<string, string> Metadata { get; init; } = new();
    }
}
