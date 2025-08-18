// Models/ImageProcessOptions.cs

namespace Functions.Models
{
    public sealed class ImageProcessOptions
    {
        public int ThumbnailMaxWidth { get; init; } = 512;
        public int ThumbnailMaxHeight { get; init; } = 512;
        public bool Overwrite { get; init; } = true;
    }
}
