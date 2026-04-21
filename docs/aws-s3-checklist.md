# S3 Checklist

Use this checklist to prepare S3 for this project.

## 1. Create the Buckets

Create two buckets:

- input bucket for uploaded source videos
- output bucket for transcoded files and manifest files

Example names:

- `mc-demo-input-123456789012-eu-central-1`
- `mc-demo-output-123456789012-eu-central-1`

Validate:

```powershell
aws s3api head-bucket --bucket mc-demo-input-123456789012-eu-central-1
aws s3api head-bucket --bucket mc-demo-output-123456789012-eu-central-1
```

Confirm:

- both commands succeed

## 2. Apply Basic Bucket Configuration

Recommended settings:

- block public access
- enable default encryption
- keep versioning optional
- optionally add lifecycle cleanup for demo data

Re-check:

- the buckets are in the intended region
- public access is blocked
- encryption is enabled

## 3. Confirm Prefix Layout

This project expects these prefixes by default:

- source uploads: `uploads/{videoId}/source/{fileName}`
- outputs: `outputs/{videoId}/...`
- manifests: `system/jobs/{videoId}.json`

Match these app settings:

- `Storage:UploadPrefix`
- `Storage:OutputPrefix`
- `Storage:ManifestPrefix`

Default values:

- `uploads`
- `outputs`
- `system/jobs`

## 4. Validate Your Own S3 Access

Your local AWS identity should be able to:

- check bucket existence
- upload objects
- download objects
- read object metadata
- write manifests to the output bucket

Validate write:

```powershell
Set-Content -Path .\tmp-s3-check.txt -Value "ok"
aws s3 cp .\tmp-s3-check.txt s3://mc-demo-output-123456789012-eu-central-1/system/healthcheck/tmp-s3-check.txt
```

Validate read:

```powershell
aws s3 cp s3://mc-demo-output-123456789012-eu-central-1/system/healthcheck/tmp-s3-check.txt .\tmp-s3-check-downloaded.txt
```

Confirm:

- upload succeeds
- download succeeds

Clean up if desired:

```powershell
aws s3 rm s3://mc-demo-output-123456789012-eu-central-1/system/healthcheck/tmp-s3-check.txt
Remove-Item .\tmp-s3-check.txt,.\tmp-s3-check-downloaded.txt
```

## 5. Re-check IAM Permissions for Your Local Caller

Your local AWS identity should have permissions equivalent to:

- `s3:ListBucket`
- `s3:GetObject`
- `s3:PutObject`
- `s3:GetBucketLocation`

Confirm:

- you can read the input bucket
- you can write to the output bucket
- you can later upload source files through the presigned flow

## 6. Re-check Bucket Policies

Confirm bucket policies do not block:

- your local caller from validation steps
- the application from writing manifests
- MediaConvert from reading input objects
- MediaConvert from writing outputs

This is especially important if the account uses restrictive bucket policies.

## 7. Final S3 Ready Checklist

S3 is ready when:

- both buckets exist
- both buckets are in the correct region
- public access settings are correct
- encryption is enabled
- prefixes are confirmed
- your local AWS identity can read and write test objects
- bucket policies do not block the expected callers
