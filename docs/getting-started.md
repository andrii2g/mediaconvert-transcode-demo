# Getting Started

This document is the short entry point for preparing AWS and running this project.

Use these documents together:

- [S3 Checklist](aws-s3-checklist.md)
- [SNS Checklist](aws-sns-checklist.md)
- [MediaConvert Checklist](aws-mediaconvert-checklist.md)

## 1. Configure AWS Credentials First

Run:

```powershell
aws configure
```

Provide:

- AWS access key ID
- AWS secret access key
- default AWS region
- default output format

Validate:

```powershell
aws sts get-caller-identity
aws configure get region
```

Confirm:

- you are logged into the expected AWS account
- the region is the one you want to use for S3, SNS, and MediaConvert

Do not continue until this works.

## 2. Local Debug Credentials for the API

The application uses the default AWS SDK credential chain.

Recommended debugging options:

- `aws configure`
- environment variables

Example:

```powershell
$env:AWS_ACCESS_KEY_ID="your-access-key-id"
$env:AWS_SECRET_ACCESS_KEY="your-secret-access-key"
$env:AWS_REGION="eu-central-1"
```

Optional:

```powershell
$env:AWS_SESSION_TOKEN="your-session-token"
```

Validate again:

```powershell
aws sts get-caller-identity
```

### About `appsettings.json`

You can store project settings in:

- [appsettings.json](src/Demo.UploadApi/appsettings.json)
- [appsettings.Development.json](src/Demo.UploadApi/appsettings.Development.json)
- environment variables

But for AWS secrets specifically:

- the current code does not read custom AWS access keys from `appsettings.json`
- use `aws configure` or environment variables for AWS credentials

Good local debugging split:

- AWS credentials from `aws configure` or environment variables
- project configuration from `appsettings.Development.json` or environment variables

## 3. Configure Project Settings

Required project settings:

- `Storage:InputBucket`
- `Storage:OutputBucket`
- `Storage:UploadPrefix`
- `Storage:OutputPrefix`
- `Storage:ManifestPrefix`
- `MediaConvert:Endpoint`
- `MediaConvert:RoleArn`
- `MediaConvert:JobTemplateName`

Recommended local debug setup:

```powershell
$env:Storage__InputBucket="mc-demo-input-123456789012-eu-central-1"
$env:Storage__OutputBucket="mc-demo-output-123456789012-eu-central-1"
$env:Storage__UploadPrefix="uploads"
$env:Storage__OutputPrefix="outputs"
$env:Storage__ManifestPrefix="system/jobs"
$env:MediaConvert__Endpoint="https://abcd1234.mediaconvert.eu-central-1.amazonaws.com"
$env:MediaConvert__RoleArn="arn:aws:iam::123456789012:role/MediaConvertDemoRole"
$env:MediaConvert__JobTemplateName="mc-demo-basic-hls-mp4"
$env:AWS_REGION="eu-central-1"
```

Re-check before running:

- bucket names are real
- prefixes are the ones you want
- MediaConvert endpoint is real
- MediaConvert role ARN is real
- MediaConvert template exists

## 4. AWS Setup Order

Use this order:

1. configure AWS credentials with `aws configure`
2. validate AWS account and region
3. prepare S3 using [S3 Checklist](aws-s3-checklist.md)
4. prepare SNS using [SNS Checklist](aws-sns-checklist.md)
5. prepare MediaConvert using [MediaConvert Checklist](aws-mediaconvert-checklist.md)
6. configure the application settings
7. run tests
8. run the API
9. execute the end-to-end flow

## 5. Build and Run

From the repository root:

```powershell
dotnet restore
dotnet test
dotnet run --project .\src\Demo.UploadApi\Demo.UploadApi.csproj
```

Confirm:

- restore succeeds
- tests pass
- the API starts

## 6. End-to-End Validation

Run this short validation:

1. call `POST /uploads`
2. confirm you receive `videoId`, `bucket`, `objectKey`, and `uploadUrl`
3. upload a `.mp4`, `.mov`, or `.mkv` file with the presigned URL
4. confirm the source object exists in S3
5. call `POST /transcodes`
6. confirm a `jobId` is returned
7. poll `GET /transcodes/{videoId}`
8. wait for `Completed`
9. call `GET /transcodes/{videoId}/result`
10. confirm outputs exist under `outputs/{videoId}/`

## 7. Final Ready Checklist

You are ready when all of these are true:

- AWS credentials are configured
- `aws sts get-caller-identity` succeeds
- the region is correct
- S3 is ready
- SNS is ready
- MediaConvert is ready
- application settings match real AWS resources
- `dotnet test` passes
- the API starts
