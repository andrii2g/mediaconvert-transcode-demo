using Demo.Contracts.Enums;

namespace Demo.UploadApi.Services;

public static class MediaConvertStatusMapper
{
    public static TranscodeJobStatus Map(string? status) =>
        status switch
        {
            "SUBMITTED" => TranscodeJobStatus.Submitted,
            "PROGRESSING" => TranscodeJobStatus.Transcoding,
            "STATUS_UPDATE" => TranscodeJobStatus.Transcoding,
            "COMPLETE" => TranscodeJobStatus.Completed,
            "ERROR" => TranscodeJobStatus.Failed,
            "CANCELED" => TranscodeJobStatus.Canceled,
            _ => TranscodeJobStatus.Submitted
        };

    public static bool IsTerminal(TranscodeJobStatus status) =>
        status is TranscodeJobStatus.Completed or TranscodeJobStatus.Failed or TranscodeJobStatus.Canceled;
}
