namespace Demo.UploadApi.Options;

public sealed class MediaConvertOptions
{
    public const string SectionName = "MediaConvert";

    public string Endpoint { get; init; } = string.Empty;
    public string RoleArn { get; init; } = string.Empty;
    public string? QueueArn { get; init; }
    public string WorkflowName { get; init; } = "MediaConvert.Transcode.Demo";
    public MediaConvertTemplateOptions Template { get; init; } = new();
}

public sealed class MediaConvertTemplateOptions
{
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<MediaConvertOutputGroupOptions> OutputGroups { get; init; } = [];
}

public sealed class MediaConvertOutputGroupOptions
{
    public string Name { get; init; } = string.Empty;
    public string Prefix { get; init; } = string.Empty;
    public string GroupType { get; init; } = "HLS";
}
