namespace Demo.UploadApi.Services;

public sealed class SourceObjectValidator
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4",
        ".mov",
        ".mkv"
    };

    public bool IsSupportedExtension(string fileName) =>
        AllowedExtensions.Contains(Path.GetExtension(fileName));

    public bool MatchesVideoSourceKey(string videoId, string sourceKey, string uploadPrefix)
    {
        var expectedPrefix = $"{uploadPrefix.TrimEnd('/')}/{videoId}/source/";
        return sourceKey.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase);
    }
}
