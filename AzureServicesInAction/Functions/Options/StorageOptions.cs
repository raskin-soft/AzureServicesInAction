// Options/StorageOptions.cs

namespace Functions.Options
{
    public sealed class StorageOptions
    {
        public string UploadsContainer { get; init; } = "uploads";
        public string ProcessedContainer { get; init; } = "images";
        public string ThumbnailsContainer { get; init; } = "thumbnails";
    }
}
