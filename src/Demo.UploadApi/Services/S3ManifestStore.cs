using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Demo.Contracts.Models;
using Demo.UploadApi.Options;
using Microsoft.Extensions.Options;

namespace Demo.UploadApi.Services;

public sealed class S3ManifestStore(
    IAmazonS3 s3,
    IOptions<StorageOptions> storageOptions,
    JsonSerializerOptions jsonOptions) : IManifestStore
{
    private readonly StorageOptions _storage = storageOptions.Value;

    public async Task<VideoManifest?> GetAsync(string videoId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await s3.GetObjectAsync(_storage.OutputBucket, _storage.BuildManifestKey(videoId), cancellationToken);
            await using var stream = response.ResponseStream;
            return await JsonSerializer.DeserializeAsync<VideoManifest>(stream, jsonOptions, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task SaveAsync(VideoManifest manifest, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(manifest, jsonOptions);
        await s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _storage.OutputBucket,
            Key = _storage.BuildManifestKey(manifest.VideoId),
            ContentBody = payload,
            ContentType = "application/json"
        }, cancellationToken);
    }
}
