using System.ComponentModel.DataAnnotations;

namespace Demo.UploadApi.Models;

public sealed record CreateUploadRequest(
    [property: Required, MinLength(1)] string FileName,
    string? ContentType);

public sealed record StartTranscodeRequest(
    [property: Required, MinLength(1)] string VideoId);
