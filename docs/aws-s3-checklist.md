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

- block public access (if needed)
- enable default encryption
- keep versioning optional
- optionally add lifecycle cleanup for demo data

Re-check:

- the buckets are in the intended region
- public access is blocked (if needed)
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

## 4. Re-check IAM Permissions for Your Local Caller

Your local AWS identity should have permissions equivalent to:

- `s3:ListBucket`
- `s3:GetObject`
- `s3:PutObject`

Validate with one IAM simulation command:

```powershell
aws iam simulate-principal-policy `
  --policy-source-arn arn:aws:iam::123456789012:user/your-user-or-role `
  --action-names s3:ListBucket s3:GetObject s3:PutObject `
  --resource-arns arn:aws:s3:::mc-demo-input-123456789012-eu-central-1 arn:aws:s3:::mc-demo-input-123456789012-eu-central-1/* arn:aws:s3:::mc-demo-output-123456789012-eu-central-1 arn:aws:s3:::mc-demo-output-123456789012-eu-central-1/*
```

Confirm:

- `EvalDecision` is `allowed` for all required actions
- `s3:ListBucket` is allowed on bucket ARNs
- `s3:GetObject` and `s3:PutObject` are allowed on object ARNs

Use the exact IAM user or role ARN that the application will run as.

## 5. Re-check Bucket Policies

Confirm bucket policies do not block:

- your local caller from validation steps
- the application from writing manifests
- MediaConvert from reading input objects
- MediaConvert from writing outputs

This is especially important if the account uses restrictive bucket policies.

## 6. Final S3 Ready Checklist

S3 is ready when:

- both buckets exist
- both buckets are in the correct region
- public access settings are correct
- encryption is enabled
- prefixes are confirmed
- IAM simulation allows `s3:ListBucket`, `s3:GetObject`, and `s3:PutObject`
- bucket policies do not block the expected callers
