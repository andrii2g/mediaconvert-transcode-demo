# MediaConvert Transcode Demo

Minimal .NET 10 demo for a two-step upload and transcode flow using only:

- ASP.NET Core API
- Amazon S3
- AWS Elemental MediaConvert

## Flow

1. `POST /uploads` returns a `videoId`, deterministic S3 key, and presigned upload URL.
2. Client uploads the file directly to S3.
3. `POST /transcodes` starts the MediaConvert job for the uploaded object.
4. `GET /transcodes/{videoId}` polls current status.
5. `GET /transcodes/{videoId}/result` returns output locations when the job is complete.

## Configuration

Set configuration in `src/Demo.UploadApi/appsettings.json` or environment variables.

Required values:

- `Storage:InputBucket`
- `Storage:OutputBucket`
- `MediaConvert:Endpoint`
- `MediaConvert:RoleArn`
- `MediaConvert:JobTemplateName`
