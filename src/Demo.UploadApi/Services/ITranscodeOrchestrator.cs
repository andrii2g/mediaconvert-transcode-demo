using Demo.Contracts.Models;

namespace Demo.UploadApi.Services;

public interface ITranscodeOrchestrator
{
    Task<VideoManifest> StartAsync(VideoManifest manifest, CancellationToken cancellationToken);
    Task<VideoManifest> RefreshStatusAsync(VideoManifest manifest, CancellationToken cancellationToken);
}
