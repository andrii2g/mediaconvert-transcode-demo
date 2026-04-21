namespace Demo.UploadApi.Options;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string InputBucket { get; init; } = string.Empty;
    public string OutputBucket { get; init; } = string.Empty;
    public string UploadPrefix { get; init; } = "uploads";
    public string OutputPrefix { get; init; } = "outputs";
    public string ManifestPrefix { get; init; } = "system/jobs";
    public int PresignedUrlExpirationMinutes { get; init; } = 15;

    public string BuildSourceKey(string videoId, string fileName) =>
        $"{UploadPrefix.TrimEnd('/')}/{videoId}/source/{fileName}";

    public string BuildOutputPrefix(string videoId) =>
        $"{OutputPrefix.TrimEnd('/')}/{videoId}/";

    public string BuildManifestKey(string videoId) =>
        $"{ManifestPrefix.TrimEnd('/')}/{videoId}.json";
}
