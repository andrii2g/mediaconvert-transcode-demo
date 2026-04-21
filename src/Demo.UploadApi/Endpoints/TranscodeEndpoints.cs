using Demo.Contracts.Enums;
using Demo.Contracts.Models;
using Demo.UploadApi.Models;
using Demo.UploadApi.Options;
using Demo.UploadApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Demo.UploadApi.Endpoints;

public static class TranscodeEndpoints
{
    public static RouteGroupBuilder MapTranscodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(string.Empty);

        group.MapPost("/uploads", CreateUploadAsync)
            .WithName("CreateUpload")
            .WithSummary("Create a presigned S3 upload URL for a new video.");

        group.MapPost("/transcodes", StartTranscodeAsync)
            .WithName("StartTranscode")
            .WithSummary("Start a MediaConvert job for an uploaded video.");

        group.MapGet("/transcodes/{videoId}", GetTranscodeStatusAsync)
            .WithName("GetTranscodeStatus")
            .WithSummary("Get the current status for a video transcode.");

        group.MapGet("/transcodes/{videoId}/result", GetTranscodeResultAsync)
            .WithName("GetTranscodeResult")
            .WithSummary("Get output locations when the transcode is complete.");

        return group;
    }

    private static async Task<Results<Ok<CreateUploadResponse>, ValidationProblem>> CreateUploadAsync(
        CreateUploadRequest request,
        IS3UploadService uploadService,
        IManifestStore manifestStore,
        SourceObjectValidator validator,
        IOptions<StorageOptions> storageOptions,
        CancellationToken cancellationToken)
    {
        var fileName = FileNameSanitizer.Normalize(request.FileName);
        if (!validator.IsSupportedExtension(fileName))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["fileName"] = ["Only .mp4, .mov, and .mkv files are supported."]
            });
        }

        var storage = storageOptions.Value;
        var now = DateTimeOffset.UtcNow;
        var videoId = Guid.CreateVersion7().ToString();
        var sourceKey = storage.BuildSourceKey(videoId, fileName);
        var expiresAt = now.AddMinutes(storage.PresignedUrlExpirationMinutes);

        var manifest = new VideoManifest
        {
            VideoId = videoId,
            SourceBucket = storage.InputBucket,
            SourceKey = sourceKey,
            JobId = null,
            OutputBucket = storage.OutputBucket,
            OutputPrefix = storage.BuildOutputPrefix(videoId),
            LastKnownStatus = TranscodeJobStatus.UploadPending,
            CreatedAtUtc = now
        };

        await manifestStore.SaveAsync(manifest, cancellationToken);

        var uploadUrl = uploadService.GetPresignedUploadUrl(
            storage.InputBucket,
            sourceKey,
            request.ContentType,
            TimeSpan.FromMinutes(storage.PresignedUrlExpirationMinutes));

        return TypedResults.Ok(new CreateUploadResponse(
            videoId,
            storage.InputBucket,
            sourceKey,
            uploadUrl.ToString(),
            expiresAt));
    }

    private static async Task<Results<Ok<TranscodeStatusResponse>, NotFound<ProblemDetails>, ValidationProblem, Conflict<ProblemDetails>>> StartTranscodeAsync(
        StartTranscodeRequest request,
        IManifestStore manifestStore,
        IS3UploadService uploadService,
        ITranscodeOrchestrator orchestrator,
        SourceObjectValidator validator,
        IOptions<StorageOptions> storageOptions,
        CancellationToken cancellationToken)
    {
        var manifest = await manifestStore.GetAsync(request.VideoId, cancellationToken);
        if (manifest is null)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Video not found",
                Detail = $"No upload manifest was found for videoId '{request.VideoId}'."
            });
        }

        if (!validator.MatchesVideoSourceKey(manifest.VideoId, manifest.SourceKey, storageOptions.Value.UploadPrefix) ||
            !validator.IsSupportedExtension(manifest.SourceKey))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["videoId"] = ["The stored upload manifest is invalid or unsupported."]
            });
        }

        var objectExists = await uploadService.ObjectExistsAsync(manifest.SourceBucket, manifest.SourceKey, cancellationToken);
        if (!objectExists)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["videoId"] = ["The source object does not exist in S3 yet."]
            });
        }

        if (!string.IsNullOrWhiteSpace(manifest.JobId))
        {
            var refreshed = await orchestrator.RefreshStatusAsync(manifest, cancellationToken);
            await manifestStore.SaveAsync(refreshed, cancellationToken);

            if (!MediaConvertStatusMapper.IsTerminal(refreshed.LastKnownStatus))
            {
                return TypedResults.Ok(ToStatusResponse(refreshed));
            }

            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Reprocessing is not supported",
                Detail = $"videoId '{request.VideoId}' already has a terminal MediaConvert job."
            });
        }

        var started = await orchestrator.StartAsync(manifest with
        {
            LastKnownStatus = TranscodeJobStatus.Uploaded
        }, cancellationToken);

        await manifestStore.SaveAsync(started, cancellationToken);
        return TypedResults.Ok(ToStatusResponse(started));
    }

    private static async Task<Results<Ok<TranscodeStatusResponse>, NotFound<ProblemDetails>>> GetTranscodeStatusAsync(
        string videoId,
        IManifestStore manifestStore,
        IS3UploadService uploadService,
        ITranscodeOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        var manifest = await manifestStore.GetAsync(videoId, cancellationToken);
        if (manifest is null)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Video not found",
                Detail = $"No upload manifest was found for videoId '{videoId}'."
            });
        }

        VideoManifest updatedManifest;
        if (string.IsNullOrWhiteSpace(manifest.JobId))
        {
            var objectExists = await uploadService.ObjectExistsAsync(manifest.SourceBucket, manifest.SourceKey, cancellationToken);
            updatedManifest = manifest with
            {
                LastKnownStatus = objectExists ? TranscodeJobStatus.Uploaded : TranscodeJobStatus.UploadPending
            };
        }
        else
        {
            updatedManifest = await orchestrator.RefreshStatusAsync(manifest, cancellationToken);
        }

        await manifestStore.SaveAsync(updatedManifest, cancellationToken);
        return TypedResults.Ok(ToStatusResponse(updatedManifest));
    }

    private static async Task<Results<Ok<TranscodeResultResponse>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> GetTranscodeResultAsync(
        string videoId,
        IManifestStore manifestStore,
        ITranscodeOrchestrator orchestrator,
        IOptions<MediaConvertOptions> mediaConvertOptions,
        CancellationToken cancellationToken)
    {
        var manifest = await manifestStore.GetAsync(videoId, cancellationToken);
        if (manifest is null)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Video not found",
                Detail = $"No upload manifest was found for videoId '{videoId}'."
            });
        }

        var updatedManifest = string.IsNullOrWhiteSpace(manifest.JobId)
            ? manifest
            : await orchestrator.RefreshStatusAsync(manifest, cancellationToken);

        await manifestStore.SaveAsync(updatedManifest, cancellationToken);

        if (updatedManifest.LastKnownStatus != TranscodeJobStatus.Completed)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Transcode not complete",
                Detail = $"The current status is '{updatedManifest.LastKnownStatus}'."
            });
        }

        return TypedResults.Ok(new TranscodeResultResponse(
            updatedManifest.VideoId,
            updatedManifest.LastKnownStatus,
            updatedManifest.OutputBucket,
            updatedManifest.OutputPrefix,
            mediaConvertOptions.Value.Template.OutputGroups
                .Select(group => new TranscodeOutputGroupResponse(
                    group.Name,
                    $"{updatedManifest.OutputPrefix}{group.Prefix.TrimStart('/')}",
                    group.GroupType))
                .ToArray()));
    }

    private static TranscodeStatusResponse ToStatusResponse(VideoManifest manifest) =>
        new(
            manifest.VideoId,
            manifest.SourceBucket,
            manifest.SourceKey,
            manifest.JobId,
            manifest.LastKnownStatus,
            manifest.OutputBucket,
            manifest.OutputPrefix,
            manifest.CreatedAtUtc,
            manifest.SubmittedAtUtc,
            manifest.CompletedAtUtc,
            manifest.LastErrorCode,
            manifest.LastErrorMessage);
}
