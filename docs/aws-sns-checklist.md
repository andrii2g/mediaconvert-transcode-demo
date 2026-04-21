# SNS Checklist

Use this checklist to prepare SNS for this project baseline.

SNS is not called by the current code, but you asked to make SNS part of the AWS setup and validation process.

## 1. Create the Topic

Example:

```powershell
aws sns create-topic --name mc-demo-workflow-events
```

Confirm:

- the command returns a topic ARN

Record:

- topic ARN
- topic name
- region

## 2. Validate Topic Access

Your local AWS identity should be able to:

- create topics
- get topic attributes
- publish test messages

Validate:

```powershell
aws sns get-topic-attributes --topic-arn <your-topic-arn>
aws sns publish --topic-arn <your-topic-arn> --message "sns health check"
```

Confirm:

- both commands succeed

## 3. Re-check Topic Policy

Confirm the topic policy allows the intended publishers and subscribers.

For a baseline setup, confirm your IAM identity has access.

For a future event-driven version, the topic policy may also need to allow:

- EventBridge
- Step Functions
- Lambda
- application roles

Validate:

```powershell
aws sns get-topic-attributes --topic-arn <your-topic-arn>
```

Re-check:

- `Policy` exists when custom access is required
- no policy denies expected publishers

## 4. Optional Subscription Validation

If you want to validate end-to-end delivery, create a test subscription.

Example email subscription:

```powershell
aws sns subscribe --topic-arn <your-topic-arn> --protocol email --notification-endpoint your-email@example.com
```

Confirm:

- the subscription is created
- the endpoint confirms the subscription
- a test publish is delivered

## 5. Region and Naming Re-check

Confirm:

- SNS topic is in the same region as the rest of the project
- the topic name is the one you intend to use later

Suggested name:

- `mc-demo-workflow-events`

## 6. Final SNS Ready Checklist

SNS is ready when:

- the topic exists
- your local caller can read topic attributes
- your local caller can publish a test message
- the topic policy is reviewed
- optional test subscriptions work if you created them
