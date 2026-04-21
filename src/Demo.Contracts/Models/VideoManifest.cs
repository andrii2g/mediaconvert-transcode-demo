using Demo.Contracts.Enums;

namespace Demo.Contracts.Models;

public sealed record VideoManifest
{
    public required string VideoId { get; init; }
    public required string SourceBucket { get; init; }
    public required string SourceKey { get; init; }
    public string? JobId { get; init; }
    public required string OutputBucket { get; init; }
    public required string OutputPrefix { get; init; }
    public required TranscodeJobStatus LastKnownStatus { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset? SubmittedAtUtc { get; init; }
    public DateTimeOffset? CompletedAtUtc { get; init; }
    public string? LastErrorCode { get; init; }
    public string? LastErrorMessage { get; init; }
}
