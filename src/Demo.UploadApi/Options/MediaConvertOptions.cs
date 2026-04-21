namespace Demo.UploadApi.Options;

public sealed class MediaConvertOptions
{
    public const string SectionName = "MediaConvert";

    public string Endpoint { get; init; } = string.Empty;
    public string RoleArn { get; init; } = string.Empty;
    public string JobTemplateName { get; init; } = string.Empty;
    public string? QueueArn { get; init; }
    public string WorkflowName { get; init; } = "MediaConvert.Transcode.Demo";
}
