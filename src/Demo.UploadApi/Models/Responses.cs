using Demo.Contracts.Enums;

namespace Demo.UploadApi.Models;

public sealed record CreateUploadResponse(
    string VideoId,
    string Bucket,
    string ObjectKey,
    string UploadUrl,
    DateTimeOffset ExpiresAtUtc);

public sealed record TranscodeStatusResponse(
    string VideoId,
    string SourceBucket,
    string SourceKey,
    string? JobId,
    TranscodeJobStatus Status,
    string OutputBucket,
    string OutputPrefix,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? ErrorCode,
    string? ErrorMessage);

public sealed record TranscodeResultResponse(
    string VideoId,
    TranscodeJobStatus Status,
    string OutputBucket,
    string OutputPrefix,
    IReadOnlyList<TranscodeOutputGroupResponse> OutputGroups);

public sealed record TranscodeOutputGroupResponse(
    string Name,
    string Prefix,
    string GroupType);
