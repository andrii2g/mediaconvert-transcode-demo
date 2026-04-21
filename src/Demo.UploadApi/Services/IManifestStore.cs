using Demo.Contracts.Models;

namespace Demo.UploadApi.Services;

public interface IManifestStore
{
    Task<VideoManifest?> GetAsync(string videoId, CancellationToken cancellationToken);
    Task SaveAsync(VideoManifest manifest, CancellationToken cancellationToken);
}
