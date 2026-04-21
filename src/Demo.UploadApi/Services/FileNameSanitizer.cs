namespace Demo.UploadApi.Services;

public static class FileNameSanitizer
{
    public static string Normalize(string fileName)
    {
        var trimmed = Path.GetFileName(fileName).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("File name must not be empty.", nameof(fileName));
        }

        return trimmed;
    }
}
