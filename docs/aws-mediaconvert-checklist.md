# MediaConvert Checklist

Use this checklist to prepare MediaConvert for this project.

NOTE: use region you have. (us-east-1 used by default)
## 1. Validate Endpoint Discovery

Run:

```powershell
aws mediaconvert describe-endpoints --region us-east-1
```

Confirm:

- the command succeeds
- at least one endpoint URL is returned

Record:

- the exact endpoint URL

Example:

```text
https://mediaconvert.us-east-1.amazonaws.com
```

This exact value must be used for:

- `MediaConvert:Endpoint`

## 2. Create the MediaConvert Service Role

Create a dedicated IAM role for MediaConvert before continuing.

This role is used by MediaConvert jobs themselves to:

- read source objects from the input bucket
- write transcoded outputs to the output bucket

The role name does not have to be exactly `MediaConvertDemoRole`. That name is only an example.

After creating the role, validate it exists:

```powershell
aws iam get-role --role-name YourActualMediaConvertRoleName
```

Record:

- the exact role name
- the exact role ARN

This exact ARN must be used for:

- `MediaConvert:RoleArn`
- the IAM simulation command for `iam:PassRole`

## 3. Re-check Your Local IAM Permissions

Your local AWS identity should have permissions equivalent to:

- `mediaconvert:DescribeEndpoints`
- `mediaconvert:ListJobTemplates`
- `mediaconvert:CreateJob`
- `mediaconvert:GetJob`
- `iam:PassRole` on the MediaConvert service role

Validate with one IAM simulation command:

```powershell
aws iam simulate-principal-policy `
  --policy-source-arn arn:aws:iam::123456789012:user/your-user-or-role `
  --action-names mediaconvert:DescribeEndpoints mediaconvert:ListJobTemplates mediaconvert:CreateJob mediaconvert:GetJob iam:PassRole `
  --resource-arns * arn:aws:iam::123456789012:role/MediaConvertDemoRole
```

Confirm:

- `EvalDecision` is `allowed` for the MediaConvert actions you need
- `iam:PassRole` is allowed for the exact MediaConvert role ARN

## 4. Re-check MediaConvert Role S3 Permissions

The MediaConvert service role should allow:

- `s3:GetObject` on input objects
- `s3:ListBucket` on the input bucket
- `s3:PutObject` on output objects

NOTE: [Setting up access for other AWS accounts to your AWS Elemental MediaConvert outputs](https://docs.aws.amazon.com/mediaconvert/latest/ug/setting-up-access-for-other-aws-accounts.html)
(and look at the policy `s3:PutObjectAcl`)

Confirm:

- input bucket policy does not block MediaConvert reads
- output bucket policy does not block MediaConvert writes

If jobs fail after submission, this is one of the first places to re-check.

## 5. Create and Validate the Job Template

Expected template name for this repo:

- `mc-demo-hls-ladder`

Create it from the repo file:

```powershell
aws mediaconvert create-job-template `
  --endpoint-url https://abcd1234.mediaconvert.us-east-1.amazonaws.com `
  --region us-east-1 `
  --cli-input-json file://infrastructure/mediaconvert/job-template-hls-ladder.json
```

If the template already exists, update it instead:

```powershell
aws mediaconvert update-job-template `
  --name mc-demo-hls-ladder `
  --endpoint-url https://abcd1234.mediaconvert.us-east-1.amazonaws.com `
  --region us-east-1 `
  --cli-input-json file://infrastructure/mediaconvert/job-template-hls-ladder.json
```

Validate template presence:

```powershell
aws mediaconvert list-job-templates --endpoint-url https://abcd1234.mediaconvert.us-east-1.amazonaws.com --region us-east-1
```

Confirm:

- the template exists
- the template name exactly matches the configured setting

Also confirm the template is compatible with this project:

- it supports three HLS output groups
- the output group names are `hls-360p`, `hls-480p`, and `hls-720p`
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
- IAM simulation allows the MediaConvert actions you need
- the MediaConvert role exists
- the MediaConvert role has correct S3 permissions
- the job template exists
- your application settings match the real endpoint, role ARN, and template name
