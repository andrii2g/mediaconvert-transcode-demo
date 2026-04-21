# Getting Started

This document is the short entry point for preparing AWS and running this project.

Use these documents together:

- [S3 Checklist](aws-s3-checklist.md)
- [SNS Checklist](aws-sns-checklist.md)
- [MediaConvert Checklist](aws-mediaconvert-checklist.md)

## 1. Configure AWS Credentials First

Before this step, make sure AWS CLI is installed and available on `PATH`.

### Windows

Install AWS CLI v2 using the official MSI:

- [AWS CLI MSI Installer](https://awscli.amazonaws.com/AWSCLIV2.msi)

Or run:

```bat
msiexec.exe /i https://awscli.amazonaws.com/AWSCLIV2.msi
```

Then reopen your terminal and verify:

```powershell
aws --version
```

### Linux

Install AWS CLI v2 using the official installer:

```bash
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o "awscliv2.zip"
unzip awscliv2.zip
sudo ./aws/install
```

Then verify:

```bash
aws --version
```

If `aws` is still not found after installation:

- reopen the terminal
- confirm the install completed successfully
- confirm the AWS CLI location is on `PATH`

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

## 2. Configure Project Settings

Required project settings: (appsettings.json)

- `Storage:InputBucket`
- `Storage:OutputBucket`
- `Storage:UploadPrefix`
- `Storage:OutputPrefix`
- `Storage:ManifestPrefix`
- `MediaConvert:Endpoint`
- `MediaConvert:RoleArn`
- `MediaConvert:Template:Name`
- `MediaConvert:Template:OutputGroups`


Re-check before running:

- bucket names are real
- prefixes are the ones you want
- MediaConvert endpoint is real
- MediaConvert role ARN is real
- MediaConvert template exists
- MediaConvert template output groups are `hls-360p`, `hls-480p`, and `hls-720p`

## 3. AWS Setup Order

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

## 4. Build and Run

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

## 5. End-to-End Validation

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

## 6. Final Ready Checklist

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
