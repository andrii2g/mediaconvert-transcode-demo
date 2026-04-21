namespace Demo.Contracts.Enums;

public enum TranscodeJobStatus
{
    UploadPending = 0,
    Uploaded = 1,
    Submitted = 2,
    Transcoding = 3,
    Completed = 4,
    Failed = 5,
    Canceled = 6
}
