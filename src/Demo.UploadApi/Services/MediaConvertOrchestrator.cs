using Amazon.MediaConvert;
using Amazon.MediaConvert.Model;
using Demo.Contracts.Enums;
using Demo.Contracts.Models;
using Demo.UploadApi.Options;
using Microsoft.Extensions.Options;

namespace Demo.UploadApi.Services;

public sealed class MediaConvertOrchestrator(
    IAmazonMediaConvert mediaConvert,
    IOptions<MediaConvertOptions> mediaConvertOptions) : ITranscodeOrchestrator
{
    private readonly MediaConvertOptions _options = mediaConvertOptions.Value;

    public async Task<VideoManifest> StartAsync(VideoManifest manifest, CancellationToken cancellationToken)
    {
        var request = new CreateJobRequest
        {
            Role = _options.RoleArn,
            JobTemplate = _options.Template.Name,
            Queue = string.IsNullOrWhiteSpace(_options.QueueArn) ? null : _options.QueueArn,
            Settings = BuildTemplateOverrides(manifest),
            UserMetadata = new Dictionary<string, string>
            {
                ["videoId"] = manifest.VideoId,
                ["sourceBucket"] = manifest.SourceBucket,
                ["sourceKey"] = manifest.SourceKey,
                ["workflowName"] = _options.WorkflowName
            }
        };

        var response = await mediaConvert.CreateJobAsync(request, cancellationToken);
        var now = DateTimeOffset.UtcNow;

        return manifest with
        {
            JobId = response.Job.Id,
            LastKnownStatus = TranscodeJobStatus.Submitted,
            SubmittedAtUtc = now,
            LastErrorCode = null,
            LastErrorMessage = null
        };
    }

    public async Task<VideoManifest> RefreshStatusAsync(VideoManifest manifest, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manifest.JobId))
        {
            return manifest;
        }

        var response = await mediaConvert.GetJobAsync(new GetJobRequest
        {
            Id = manifest.JobId
        }, cancellationToken);

        var snapshot = new MediaConvertJobSnapshot(
            response.Job.Id,
            MediaConvertStatusMapper.Map(response.Job.Status),
            response.Job.Timing?.FinishTime is { } finishTime ? new DateTimeOffset(finishTime.ToUniversalTime()) : null,
            response.Job.ErrorCode?.ToString(),
            response.Job.ErrorMessage);

        return manifest with
        {
            LastKnownStatus = snapshot.Status,
            CompletedAtUtc = snapshot.CompletedAtUtc ?? manifest.CompletedAtUtc,
            LastErrorCode = snapshot.ErrorCode,
            LastErrorMessage = snapshot.ErrorMessage
        };
    }

    private JobSettings BuildTemplateOverrides(VideoManifest manifest)
    {
        var baseOutputPath = $"s3://{manifest.OutputBucket}/{manifest.OutputPrefix}";

        return new JobSettings
        {
            Inputs =
            [
                new Input
                {
                    FileInput = $"s3://{manifest.SourceBucket}/{manifest.SourceKey}"
                }
            ],
            OutputGroups = [.. _options.Template.OutputGroups.Select(group => BuildOutputGroup(baseOutputPath, group))]
        };
    }

    private static OutputGroup BuildOutputGroup(string baseOutputPath, MediaConvertOutputGroupOptions group)
    {
        var destination = $"{baseOutputPath}{group.Prefix.TrimStart('/')}";

        return group.GroupType.ToUpperInvariant() switch
        {
            "HLS" => new OutputGroup
            {
                Name = group.Name,
                OutputGroupSettings = new OutputGroupSettings
                {
                    Type = OutputGroupType.HLS_GROUP_SETTINGS,
                    HlsGroupSettings = new HlsGroupSettings
                    {
                        Destination = destination
                    }
                }
            },
            "FILE" => new OutputGroup
            {
                Name = group.Name,
                OutputGroupSettings = new OutputGroupSettings
                {
                    Type = OutputGroupType.FILE_GROUP_SETTINGS,
                    FileGroupSettings = new FileGroupSettings
                    {
                        Destination = destination
                    }
                }
            },
            _ => throw new InvalidOperationException($"Unsupported MediaConvert output group type '{group.GroupType}'.")
        };
    }
}
