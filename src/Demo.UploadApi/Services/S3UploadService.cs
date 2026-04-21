using Amazon.S3;
using Amazon.S3.Model;

namespace Demo.UploadApi.Services;

public sealed class S3UploadService(IAmazonS3 s3) : IS3UploadService
{
    public Uri GetPresignedUploadUrl(string bucket, string objectKey, string? contentType, TimeSpan expiresIn)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = bucket,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.Add(expiresIn),
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType
        };

        return new Uri(s3.GetPreSignedURL(request));
    }

    public async Task<bool> ObjectExistsAsync(string bucket, string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            await s3.GetObjectMetadataAsync(bucket, objectKey, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
