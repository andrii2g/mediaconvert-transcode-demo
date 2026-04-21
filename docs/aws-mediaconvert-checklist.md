# MediaConvert Checklist

Use this checklist to prepare MediaConvert for this project.

## 1. Validate Endpoint Discovery

Run:

```powershell
aws mediaconvert describe-endpoints --region eu-central-1
```

Confirm:

- the command succeeds
- at least one endpoint URL is returned

Record:

- the exact endpoint URL

Example:

```text
https://abcd1234.mediaconvert.eu-central-1.amazonaws.com
```

This exact value must be used for:

- `MediaConvert:Endpoint`

## 2. Re-check Your Local IAM Permissions

Your local AWS identity should have permissions equivalent to:

- `mediaconvert:DescribeEndpoints`
- `mediaconvert:ListJobTemplates`
- `mediaconvert:CreateJob`
- `mediaconvert:GetJob`
- `iam:PassRole` on the MediaConvert service role

Validate the basic access:

```powershell
aws mediaconvert describe-endpoints --region eu-central-1
aws mediaconvert list-job-templates --endpoint-url https://abcd1234.mediaconvert.eu-central-1.amazonaws.com --region eu-central-1
```

Confirm:

- both commands succeed

## 3. Create the MediaConvert Service Role

Create a dedicated IAM role for MediaConvert.

This role is used by MediaConvert jobs themselves.

Confirm the role exists:

```powershell
aws iam get-role --role-name MediaConvertDemoRole
```

Record:

- the exact role ARN

This exact value must be used for:

- `MediaConvert:RoleArn`

## 4. Re-check MediaConvert Role S3 Permissions

The MediaConvert service role should allow:

- `s3:GetObject` on input objects
- `s3:ListBucket` on the input bucket
- `s3:PutObject` on output objects
- `s3:GetBucketLocation` where required

Confirm:

- input bucket policy does not block MediaConvert reads
- output bucket policy does not block MediaConvert writes

If jobs fail after submission, this is one of the first places to re-check.

## 5. Create and Validate the Job Template

Expected template name for this repo:

- `mc-demo-basic-hls-mp4`

Validate template presence:

```powershell
aws mediaconvert list-job-templates --endpoint-url https://abcd1234.mediaconvert.eu-central-1.amazonaws.com --region eu-central-1
```

Confirm:

- the template exists
- the template name exactly matches the configured setting

Also confirm the template is compatible with this project:

- it supports one HLS output group
- it supports one MP4/file output group
- it can work with S3 input and S3 output destinations

## 6. Re-check Ability to Start Jobs

Before running the app, confirm all of these are true:

- endpoint discovery works
- template listing works
- your local caller has `iam:PassRole`
- the MediaConvert role ARN is correct
- the MediaConvert template name is correct

Practical confirmation:

- the first `POST /transcodes` call should not fail with `AccessDenied`

If it does fail, re-check:

- local IAM policy
- `iam:PassRole`
- endpoint URL
- role ARN
- template name

## 7. Final MediaConvert Ready Checklist

MediaConvert is ready when:

- endpoint discovery succeeds
- the endpoint URL is recorded
- your local caller can list templates
- the MediaConvert role exists
- the MediaConvert role has correct S3 permissions
- the job template exists
- your application settings match the real endpoint, role ARN, and template name
