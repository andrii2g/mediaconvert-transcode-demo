namespace Demo.UploadApi.Services;

public interface IS3UploadService
{
    Uri GetPresignedUploadUrl(string bucket, string objectKey, string? contentType, TimeSpan expiresIn);
    Task<bool> ObjectExistsAsync(string bucket, string objectKey, CancellationToken cancellationToken);
}
