# PLAN.md — MediaConvert Transcoding Demo (.NET 10)

## 1. Purpose

Build a small but production-shaped demo that shows an end-to-end AWS media pipeline:

1. A client uploads a video to Amazon S3.
2. S3 emits an event when the object is created.
3. The workflow sends an SNS notification that upload completed.
4. The workflow starts an AWS Elemental MediaConvert job with predefined settings.
5. MediaConvert emits status events during processing.
6. EventBridge routes those events to SNS so subscribers can observe progress.
7. When transcoding completes or fails, SNS sends the final notification.

### Main objective

Keep the demo simple, observable, and easy to extend.

### Non-goals for v1

- No custom database for job state
- No user-defined transcoding profiles
- No authentication/authorization layer
- No UI dashboard
- No CloudFront distribution
- No DRM
- No subtitle workflow
- No persistence of a custom "status record"

---

## 2. Final Recommendation

Use this architecture:

```text
Client
  -> upload video to S3 input bucket

S3 Object Created event
  -> EventBridge rule
      -> SNS topic: UploadCompleted
      -> Step Functions state machine: StartTranscodeWorkflow

Step Functions
  -> Prepare job metadata
  -> Submit MediaConvert CreateJob
  -> Publish SNS topic: MediaConvertJobSubmitted
  -> Complete execution

MediaConvert job events
  -> EventBridge rules
      -> SNS topic: MediaConvertProgress
      -> SNS topic: MediaConvertCompleted
      -> SNS topic: MediaConvertFailed
```

### Why this is the best fit for the demo

- **S3** is the source of truth for upload completion.
- **EventBridge** is the event router.
- **SNS** is the notification fanout layer.
- **Step Functions** is the workflow orchestrator.
- **MediaConvert** performs the transcoding work.
- **No Lambda is required in v1** unless you later need custom logic that Step Functions cannot cleanly handle.

---

## 3. Guiding Principles

1. **Use event-driven boundaries**
   - Do not start transcoding from the client application directly after upload.
   - Start from the S3 object-created event.

2. **Use predefined transcoding profiles only**
   - Avoid exposing arbitrary codec/bitrate/frame-size settings in v1.

3. **Keep SNS as a notification channel, not a workflow database**
   - SNS messages are notifications.
   - Current status is inferable from the latest notification stream and observable in Step Functions + MediaConvert.
   - If the system later needs “query current status now,” add DynamoDB in v2.

4. **Prefer deterministic S3 key conventions**
   - Make input and output paths predictable.

5. **Use Infrastructure as Code**
   - All infrastructure must be reproducible from source.

6. **Design for idempotency**
   - Event delivery can be repeated.
   - The workflow must avoid duplicate MediaConvert jobs for the same source object.

---

## 4. High-Level Architecture

## 4.1 AWS Services

### Required
- Amazon S3
- Amazon EventBridge
- Amazon SNS
- AWS Step Functions
- AWS Elemental MediaConvert
- IAM
- Amazon CloudWatch Logs

### Optional later
- DynamoDB for queryable current status
- API Gateway + ASP.NET Core API for presigned uploads
- CloudFront for output delivery
- EventBridge archive/replay
- Dead-letter queues for notification consumers

---

## 5. Repository Shape

```text
MediaConvert.Transcode.Demo/
├─ README.md
├─ PLAN.md
├─ .gitignore
├─ global.json
├─ Directory.Build.props
├─ docs/
│  ├─ architecture.md
│  ├─ workflow.md
│  ├─ aws-setup.md
│  ├─ event-contracts.md
│  └─ runbook.md
├─ src/
│  ├─ Demo.Contracts/
│  │  ├─ Events/
│  │  ├─ Notifications/
│  │  └─ Models/
│  ├─ Demo.Tooling/
│  │  ├─ Program.cs
│  │  ├─ Commands/
│  │  └─ Services/
│  └─ Demo.UploadApi/                 # optional in v1; include only if you want presigned uploads
│     ├─ Program.cs
│     ├─ Endpoints/
│     └─ Services/
├─ infrastructure/
│  ├─ cdk/
│  │  ├─ src/
│  │  └─ README.md
│  ├─ cloudformation/
│  │  ├─ stack.yaml
│  │  └─ parameters.example.json
│  ├─ eventbridge/
│  │  ├─ s3-object-created-rule.json
│  │  ├─ mediaconvert-progress-rule.json
│  │  ├─ mediaconvert-complete-rule.json
│  │  └─ mediaconvert-error-rule.json
│  ├─ stepfunctions/
│  │  └─ transcode-workflow.asl.json
│  ├─ mediaconvert/
│  │  ├─ job-template-basic-hls-mp4.json
│  │  └─ output-presets/
│  └─ sns/
│     ├─ topic-policies.md
│     └─ subscriptions.example.md
└─ samples/
   ├─ upload.http
   ├─ aws-cli/
   └─ sample-events/
```

### Recommendation
For this demo, choose **AWS CDK for .NET 10** or **CloudFormation**.
If the goal is easiest readability, use CloudFormation.
If the goal is a richer .NET-centric repo, use CDK in C#.

**Final recommendation for this repo:** use **CDK in C#** and keep generated templates out of source control unless needed for review.

---

## 6. Workflow Design

## 6.1 End-to-end flow

```text
1. Client uploads file to S3 input bucket
2. S3 emits Object Created event
3. EventBridge receives the event
4. EventBridge fans out:
   a. SNS notification: upload completed
   b. Step Functions state machine execution starts
5. Step Functions prepares MediaConvert job payload
6. Step Functions calls MediaConvert CreateJob
7. Step Functions publishes SNS notification: job submitted
8. MediaConvert starts processing
9. MediaConvert emits EventBridge status events
10. EventBridge routes:
    a. PROGRESSING / STATUS_UPDATE -> SNS progress topic
    b. COMPLETE -> SNS completed topic
    c. ERROR / CANCELED -> SNS failed topic
```

---

## 7. Final Infrastructure Design

## 7.1 S3 buckets

Use two buckets:

- `mc-demo-input-{accountId}-{region}`
- `mc-demo-output-{accountId}-{region}`

### Input bucket responsibilities
- receive uploaded source videos
- emit object-created events via EventBridge

### Output bucket responsibilities
- receive MediaConvert outputs
- keep transcoded assets separate from source inputs

### Best practices
- block public access
- enable server-side encryption
- enable versioning if desired
- apply lifecycle rules for demo cleanup
- do not mix source and output data in one bucket unless you intentionally simplify the demo

## 7.2 S3 key conventions

Use a deterministic input path:

```text
uploads/{videoId}/source/{originalFileName}
```

Example:

```text
uploads/7e8a6331-9a78-48e9-a17f-e5310e1f767d/source/input.mp4
```

Use deterministic output paths:

```text
outputs/{videoId}/hls/
outputs/{videoId}/mp4/
outputs/{videoId}/thumbnails/
```

### Why this matters
- easier troubleshooting
- easier reuse of `videoId`
- easier to derive output path from input key
- easier idempotency rules

---

## 8. Eventing Design

## 8.1 Event source: S3 upload complete

Use the S3 **Object Created** event.
This works for:
- PutObject
- Post
- Copy
- CompleteMultipartUpload

That means large multipart uploads are also handled correctly.

### Important
Do not rely on the client saying “upload is done.”
The authoritative trigger is the S3 object-created event.

## 8.2 Event router: EventBridge

Enable S3 integration with EventBridge for the input bucket.

Use EventBridge rules to match:
- object creation in the input bucket
- MediaConvert status events

### Why EventBridge
- clean routing
- multiple targets
- rich event filtering
- native integration with S3 and MediaConvert

## 8.3 Notification layer: SNS

Use SNS topics for fanout notifications.

### Recommended topics

Option A: one topic per lifecycle category
- `mc-demo-upload-completed`
- `mc-demo-job-submitted`
- `mc-demo-job-progress`
- `mc-demo-job-completed`
- `mc-demo-job-failed`

Option B: one consolidated topic
- `mc-demo-workflow-events`

### Final recommendation
Use **one consolidated topic** in v1:

- `mc-demo-workflow-events`

Reason:
- simpler demo
- fewer resources
- easier subscription management

Include `eventType` in every message so subscribers can filter client-side or via subscription policies.

---

## 9. Step Functions Design

## 9.1 Role of Step Functions

Step Functions orchestrates the submit phase only:

1. Accepts the S3 event input
2. Validates and transforms metadata
3. Derives `videoId`
4. Builds MediaConvert job request
5. Calls MediaConvert `CreateJob`
6. Publishes SNS notification `MediaConvert.JobSubmitted`
7. Ends

### Why not wait for completion in the same execution?

For this demo, do **not** keep the Step Functions execution open until MediaConvert finishes.

Reason:
- MediaConvert already emits EventBridge events
- the demo wants SNS progress notifications from MediaConvert events
- shorter executions are simpler and cheaper to reason about
- less workflow complexity

### v2 option
In a later version, Step Functions can wait for completion and add richer orchestration logic.

## 9.2 State machine outline

```text
Start
  -> NormalizeInput
  -> ValidateInput
  -> BuildJobRequest
  -> SubmitMediaConvertJob
  -> PublishJobSubmittedNotification
  -> Success
```

### Suggested states

#### NormalizeInput
- extracts bucket name
- extracts object key
- URL-decodes object key if required
- derives `videoId` from the key structure

#### ValidateInput
Reject files that:
- are in unsupported prefixes
- are empty
- do not have allowed extensions
- are outside demo constraints

Allowed extensions example:
- `.mp4`
- `.mov`
- `.mkv`

#### BuildJobRequest
Build MediaConvert payload with:
- input URI
- output base path
- queue ARN
- IAM role ARN
- job template name or full settings
- user metadata

#### SubmitMediaConvertJob
Use the Step Functions native MediaConvert integration.

#### PublishJobSubmittedNotification
Publish SNS message with:
- input bucket
- input key
- `videoId`
- MediaConvert job ID
- submit timestamp

---

## 10. MediaConvert Design

## 10.1 Use job templates

Use a predefined MediaConvert job template rather than constructing the full settings payload dynamically each time.

### Why
- easier maintenance
- predictable outputs
- easier documentation
- safer demo behavior

### Recommended template name
- `mc-demo-basic-hls-mp4`

## 10.2 Initial output profile

Start with exactly these outputs:

1. **HLS package**
2. **Single MP4 output**
3. **Optional thumbnails** (only if you want more visible demo results)

### Final v1 recommendation
Use:
- HLS
- MP4

Skip thumbnails unless specifically needed.

## 10.3 MediaConvert queue

Use the default on-demand queue for the demo unless you have a reason to isolate workloads.

### Recommendation
- default queue for simplicity
- custom queue only in v2

## 10.4 Job metadata

Populate MediaConvert `UserMetadata` with:
- `videoId`
- `sourceBucket`
- `sourceKey`
- `workflowName`
- `submittedBy=StepFunctions`

This helps correlate MediaConvert events later.

---

## 11. SNS Notification Design

## 11.1 Notification principles

Every SNS notification should be:
- explicit
- self-describing
- correlation-friendly
- safe to consume independently

Each message should contain enough context so a subscriber does not need another lookup.

## 11.2 Message contract

Use a common envelope:

```json
{
  "eventType": "MediaConvert.StatusUpdate",
  "version": 1,
  "timestampUtc": "2026-04-21T12:00:00Z",
  "correlationId": "7e8a6331-9a78-48e9-a17f-e5310e1f767d",
  "workflow": "MediaConvert.Transcode.Demo",
  "source": {
    "bucket": "mc-demo-input-123456789012-eu-central-1",
    "key": "uploads/7e8a6331-9a78-48e9-a17f-e5310e1f767d/source/input.mp4"
  },
  "mediaConvert": {
    "jobId": "1713797000000-abcd12",
    "status": "STATUS_UPDATE",
    "phase": "TRANSCODING",
    "jobPercentComplete": 42,
    "phasePercentComplete": 63
  },
  "outputs": {
    "bucket": "mc-demo-output-123456789012-eu-central-1",
    "basePrefix": "outputs/7e8a6331-9a78-48e9-a17f-e5310e1f767d/"
  },
  "details": {
    "message": "MediaConvert job is progressing."
  }
}
```

## 11.3 Event types

Use these event type names:

- `S3.UploadCompleted`
- `Workflow.TranscodeStarted`
- `MediaConvert.JobSubmitted`
- `MediaConvert.Progressing`
- `MediaConvert.StatusUpdate`
- `MediaConvert.Completed`
- `MediaConvert.Failed`
- `MediaConvert.Canceled`

### Recommendation
Keep event names stable from the start.

## 11.4 SNS message attributes

In addition to the message body, include message attributes where useful:

- `eventType`
- `videoId`
- `jobStatus`
- `workflow`

This helps subscribers filter efficiently.

---

## 12. EventBridge Rule Design

## 12.1 Rule: S3 object created in input bucket

### Match conditions
- source = `aws.s3`
- detail-type = `Object Created`
- bucket name = input bucket
- key prefix = `uploads/`

### Targets
- SNS topic `mc-demo-workflow-events`
- Step Functions state machine `mc-demo-start-transcode`

### Transformer for SNS
Transform the S3 event into the common notification envelope.

### Important
Do not subscribe MediaConvert directly from S3.
Use EventBridge as the router.

## 12.2 Rule: MediaConvert progressing/status update

### Match conditions
- source = `aws.mediaconvert`
- detail.status in `["PROGRESSING", "STATUS_UPDATE"]`

### Target
- SNS topic `mc-demo-workflow-events`

## 12.3 Rule: MediaConvert completed

### Match conditions
- source = `aws.mediaconvert`
- detail.status = `COMPLETE`

### Target
- SNS topic `mc-demo-workflow-events`

## 12.4 Rule: MediaConvert failed or canceled

### Match conditions
- source = `aws.mediaconvert`
- detail.status in `["ERROR", "CANCELED"]`

### Target
- SNS topic `mc-demo-workflow-events`

---

## 13. Idempotency and Duplicate Event Handling

This is mandatory.

S3 and EventBridge deliveries can be repeated.
The system must not create duplicate MediaConvert jobs for the same input object unless that behavior is explicitly intended.

## 13.1 v1 strategy

Use one of these approaches:

### Preferred
Derive a deterministic execution name for the Step Functions state machine from:
- bucket
- key
- object version ID or ETag if available

Then reject duplicate execution starts for the same object.

### Alternative
Allow duplicate Step Functions executions but prevent duplicate job submission by checking a marker object in S3:
- write a marker under `system/submitted/{videoId}.json`
- skip if it already exists

### Final recommendation
Use **Step Functions execution naming + deterministic `videoId`** first.
If your upload process can replace the same key with a new file, include ETag or version ID where possible.

---

## 14. Input Validation Rules

Apply minimal validation before job submission.

## Required
- file extension allowlist
- expected key prefix
- non-empty key
- no hidden/system prefixes

## Optional
- object size lower/upper bounds
- allowed content type
- source bucket allowlist
- supported file name character set

### Recommendation
For v1, validate:
- prefix
- extension
- non-empty object

Do not overengineer validation.

---

## 15. .NET 10 Implementation Guidance

## 15.1 Runtime and SDK

Use:
- **.NET 10**
- **AWS SDK for .NET V4** for custom tooling and helper applications

### Why
- .NET 10 is the current LTS release.
- AWS SDK for .NET V4 is GA and appropriate for new projects.

## 15.2 Project style

Use:
- nullable enabled
- implicit usings enabled
- central package management if desired
- analyzers enabled
- warnings as errors in CI
- `async` all the way
- structured logging

## 15.3 Solution layout

### Demo.Contracts
Contains:
- notification DTOs
- common constants
- event type names
- JSON serialization options

### Demo.Tooling
Console app for:
- generating test upload paths
- optionally creating presigned URLs
- test publishing sample notifications
- operational helpers

### Demo.UploadApi (optional)
Minimal ASP.NET Core API for:
- generating presigned upload URLs
- simple health endpoint

### Recommendation
For a pure AWS demo, `Demo.Tooling` is enough.
Add `Demo.UploadApi` only if you want a cleaner upload UX.

## 15.4 JSON handling

Use:
- `System.Text.Json`
- explicit record types
- versioned contracts
- ISO 8601 UTC timestamps

Do not use anonymous dynamic JSON objects for core contracts.

## 15.5 Logging

Use structured logging with:
- bucket
- key
- videoId
- mediaConvertJobId
- eventType

Never log only raw strings without structured fields.

---

## 16. Infrastructure as Code Guidance

## 16.1 CDK stack contents

One stack is enough for v1.

### Resources
- input S3 bucket
- output S3 bucket
- SNS topic
- Step Functions state machine
- EventBridge rules
- MediaConvert IAM role
- Step Functions IAM role
- CloudWatch log groups

## 16.2 IAM design

Use least privilege.

### Step Functions execution role needs:
- `mediaconvert:CreateJob`
- `iam:PassRole` for the MediaConvert service role
- `sns:Publish` to the SNS topic
- `logs:*` as required by the service integration/logging setup

### MediaConvert service role needs:
- `s3:GetObject` on input bucket/prefix
- `s3:ListBucket` on input bucket as required
- `s3:PutObject` on output bucket/prefix
- `s3:GetBucketLocation` where required

### EventBridge target permissions
Grant EventBridge permission to:
- start the state machine
- publish to SNS

## 16.3 Logging and retention

Set log retention explicitly for:
- Step Functions logs
- application logs if any

For demo defaults:
- 14 days or 30 days

---

## 17. Recommended Step Functions ASL Shape

Use a concise state machine.

Example logical shape:

```json
{
  "Comment": "Start MediaConvert job for uploaded S3 object",
  "StartAt": "NormalizeInput",
  "States": {
    "NormalizeInput": {
      "Type": "Pass",
      "Next": "ValidateInput"
    },
    "ValidateInput": {
      "Type": "Choice",
      "Choices": [
        {
          "Variable": "$.normalized.isSupported",
          "BooleanEquals": true,
          "Next": "BuildJobRequest"
        }
      ],
      "Default": "UnsupportedInput"
    },
    "BuildJobRequest": {
      "Type": "Pass",
      "Next": "SubmitMediaConvertJob"
    },
    "SubmitMediaConvertJob": {
      "Type": "Task",
      "Next": "PublishJobSubmittedNotification"
    },
    "PublishJobSubmittedNotification": {
      "Type": "Task",
      "Next": "Success"
    },
    "UnsupportedInput": {
      "Type": "Succeed"
    },
    "Success": {
      "Type": "Succeed"
    }
  }
}
```

### Note
Actual ASL will depend on whether you use:
- JSONPath
- JSONata
- CDK helper abstractions

Keep the first version straightforward.

---

## 18. Upload Strategy

## 18.1 Best practice
Use presigned S3 upload URLs so the client uploads directly to S3.

### Benefits
- app server stays out of the data path
- simpler architecture
- lower cost
- better scalability

## 18.2 v1 options

### Option A
Use the AWS Console manually for uploads during demo setup.

### Option B
Use a small .NET 10 console tool to generate presigned URLs.

### Option C
Use a tiny ASP.NET Core minimal API that returns a presigned PUT URL.

### Final recommendation
Implement **Option B first**:
- easiest repo
- still demonstrates .NET 10 code
- avoids building extra API infrastructure

---

## 19. MediaConvert Preset and Template Guidance

## 19.1 Keep templates in source control

Store the template JSON under:

```text
infrastructure/mediaconvert/job-template-basic-hls-mp4.json
```

## 19.2 Configuration strategy

Use infrastructure parameters for:
- input bucket name
- output bucket name
- SNS topic ARN
- MediaConvert queue ARN
- MediaConvert role ARN

Do not hardcode account IDs or regions in app code.

## 19.3 Avoid runtime template mutation
Do not dynamically rewrite codec parameters in v1.
Only set:
- input file
- destination path
- metadata

---

## 20. Observability

## 20.1 Primary observability surfaces
- SNS notifications
- EventBridge rule metrics
- Step Functions execution history
- MediaConvert job history
- CloudWatch logs

## 20.2 Correlation IDs

Use the same `videoId` everywhere:
- S3 key path
- SNS messages
- Step Functions execution input
- MediaConvert user metadata
- logs

This is critical.

## 20.3 Failure analysis
For any failure, you should be able to answer:
- what source object triggered the workflow?
- was Step Functions started?
- was MediaConvert job submitted?
- what job ID was assigned?
- what final status occurred?
- which SNS notifications were published?

---

## 21. Error Handling

## 21.1 Unsupported input
If input fails validation:
- do not submit MediaConvert job
- optionally publish an SNS notification `Workflow.InputRejected`
- finish successfully from a workflow perspective

## 21.2 MediaConvert create job failure
If `CreateJob` fails:
- publish SNS notification `MediaConvert.SubmitFailed`
- fail the Step Functions execution

## 21.3 EventBridge routing failure
Monitor rule invocations and failed invocations.
If reliability becomes important, add dead-letter support in v2.

## 21.4 SNS subscriber failure
SNS delivery failure affects the subscriber, not the producer.
For demo purposes this is acceptable.

---

## 22. Security Guidance

## 22.1 S3
- Block public access
- SSE-S3 or SSE-KMS
- least-privilege bucket policies
- lifecycle cleanup if uploads are temporary

## 22.2 IAM
- no wildcard permissions unless unavoidable
- no administrator roles in demo runtime
- separate roles for Step Functions and MediaConvert

## 22.3 Secrets
No secrets should be required in v1 if uploads are done by AWS Console or presigned URLs.

## 22.4 Regions
Keep all services in one region for v1.

### Recommended region
Pick the region where MediaConvert and your preferred AWS services are available and where your account is already active.

---

## 23. Testing Strategy

## 23.1 Unit tests
Test:
- `videoId` derivation
- key parsing
- notification serialization
- input validation

## 23.2 Contract tests
Validate notification JSON shape against sample payloads.

## 23.3 Manual integration tests
1. Upload supported file
2. Confirm upload notification
3. Confirm Step Functions execution started
4. Confirm MediaConvert job created
5. Confirm progress notifications
6. Confirm completion notification
7. Confirm outputs exist in output bucket

## 23.4 Failure-path tests
- upload unsupported file extension
- upload file into wrong prefix
- use broken MediaConvert role
- use invalid template name

---

## 24. CI/CD Guidance

## 24.1 GitHub Actions
Include workflows for:
- build
- test
- format/lint
- optional infrastructure validation

## 24.2 Build steps
- restore
- build
- test
- `dotnet format --verify-no-changes` if desired

## 24.3 Infrastructure validation
- synth CDK
- validate CloudFormation
- JSON lint ASL and template files

---

## 25. Documentation Deliverables

Create these docs early:

### `docs/architecture.md`
- architecture diagram
- service responsibilities
- why SNS/EventBridge/Step Functions are all used

### `docs/workflow.md`
- exact event flow
- step-by-step lifecycle

### `docs/event-contracts.md`
- SNS message shapes
- eventType catalog
- sample payloads

### `docs/aws-setup.md`
- bootstrap steps
- required IAM notes
- deployment steps

### `docs/runbook.md`
- how to upload a file
- how to watch Step Functions
- how to view MediaConvert jobs
- how to subscribe an email endpoint to SNS
- how to clean up resources

---

## 26. Suggested Minimal .NET 10 Deliverables

For the first implementation, include:

1. **CDK stack in C#**
   - creates all infrastructure

2. **Demo.Contracts**
   - strongly typed event payloads

3. **Demo.Tooling**
   - command to generate upload keys
   - optional command to create presigned upload URLs
   - optional command to print sample SNS payloads

4. **Infrastructure JSON**
   - Step Functions ASL
   - MediaConvert job template

This is enough for a strong repository.

---

## 27. Recommended Implementation Order

## Phase 1 — Repository skeleton
- create solution
- add docs folders
- add contracts project
- add tooling project
- add infrastructure folder structure

## Phase 2 — Infrastructure baseline
- create S3 buckets
- create SNS topic
- create EventBridge rules
- create IAM roles
- create Step Functions state machine

## Phase 3 — MediaConvert setup
- create MediaConvert role
- create job template
- wire Step Functions to `CreateJob`

## Phase 4 — Event notifications
- route S3 object-created event to SNS + Step Functions
- route MediaConvert events to SNS
- verify payload shapes

## Phase 5 — .NET tooling
- implement upload key generator
- optionally add presigned URL generator
- add sample commands and docs

## Phase 6 — Validation and hardening
- add validation rules
- add idempotency strategy
- add tests
- finalize runbook

---

## 28. Explicit Final Decisions

These are the final architectural decisions for v1:

- **Use .NET 10**
- **Use AWS SDK for .NET V4**
- **Use S3 as upload destination**
- **Use EventBridge as central event router**
- **Use one SNS topic for workflow notifications**
- **Use Step Functions to submit MediaConvert jobs**
- **Use MediaConvert EventBridge events for progress/completion/failure**
- **Use a predefined MediaConvert job template**
- **Do not use Lambda in v1**
- **Do not store workflow state in DynamoDB in v1**
- **Do not make SNS the authoritative state store**
- **Do not expose arbitrary transcoding parameters in v1**
- **Use direct-to-S3 uploads where practical**
- **Use deterministic S3 key conventions with `videoId`**
- **Use least-privilege IAM**
- **Use Infrastructure as Code**

---

## 29. Future Extensions

Good v2 ideas:

- add DynamoDB current-status projection
- add API for status queries
- add web UI
- add CloudFront playback URLs
- add thumbnails and poster frame generation
- add multiple job templates
- add dead-letter queues
- add email + HTTPS subscriber examples
- add EventBridge archive/replay
- add signed uploads with auth
- add support for webhook notifications
- add parallel output profiles

---

## 30. Final Summary

This demo should be built as an event-driven AWS workflow with these responsibilities:

- **S3** detects upload completion
- **EventBridge** routes lifecycle events
- **SNS** broadcasts workflow notifications
- **Step Functions** orchestrates MediaConvert job submission
- **MediaConvert** performs transcoding and emits progress/final events

That keeps the design:
- simple
- observable
- extensible
- aligned with AWS service boundaries
- realistic enough to be a valuable GitHub demo project
