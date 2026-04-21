using Demo.Contracts.Enums;

namespace Demo.UploadApi.Services;

public sealed record MediaConvertJobSnapshot(
    string JobId,
    TranscodeJobStatus Status,
    DateTimeOffset? CompletedAtUtc,
    string? ErrorCode,
    string? ErrorMessage);
