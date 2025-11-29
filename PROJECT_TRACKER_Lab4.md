# Lab 4 — Project Tracker

This tracker maps the lab rubric to concrete tasks, acceptance criteria, and suggested next steps so you can work through the assignment methodically.

**How to use this file**
- Update the status of each task as you make progress (not-started / in-progress / completed).
- Keep notes under each task for commands, ARNs, or screenshots.
- The canonical todo list is also stored in the repo's task tracker (used by the coding assistant).

---

**Overview & Priorities (Student-level)**
- **Priority 1**: Get a simple S3 → Lambda → DynamoDB flow working using the AWS Console for most steps.
- **Priority 2**: Add thumbnail generation (can be in the same Lambda to keep things simple).
- **Priority 3**: (Optional) Add Step Functions only if the basic flow is working and you feel comfortable.
- **Priority 4**: Record the demo video, take the required screenshot, and prepare the ZIP submission.

---

## Student-level Tasks (simplified)

1. Create DynamoDB Table (Console)

- Status: completed
- Notes: Table `ImageMetadataTable` was created in the AWS Console. Paste the table ARN here when available: __
- What I did: Created the table with partition key `ImageId` (String) and on-demand capacity.

2. Simple Data Model

- Status: completed
- Goal: produce a concise, testable data model you will use when writing items to `ImageMetadataTable`.
- Minimal schema (used):
  - `ImageId` (string) — DynamoDB partition key
  - `ObjectUrl` (string)
  - `DetectedLabels` (list of objects { `Name` (string), `Confidence` (number) })
  - `ThumbnailUrl` (string, optional)
  - `UploadTimestamp` (string, ISO datetime)
  - Optional: `S3Bucket` (string), `S3Key` (string)

- What I ran to verify: created a temporary console test project `tools/serialize-test` that references your main project and serialized an `Image` instance using `System.Text.Json`.

- Result: serialization succeeded and produced the expected JSON structure (example printed during test):

```json
{
  "ImageId": "img-12345",
  "ObjectUrl": "s3://mybucket/path/image.jpg",
  "DetectedLabels": [
    { "Name": "Person", "Confidence": 98.7 },
    { "Name": "Outdoor", "Confidence": 92.1 }
  ],
  "ThumbNailUrl": "s3://mybucket/path/thumb.jpg",
  "UploadTimestamp": "2025-11-29T19:39:22.8085784Z",
  "S3Bucket": "mybucket",
  "S3Key": "path/image.jpg"
}
```

- Acceptance criteria (met):
  - `models/Image.cs` includes `ImageId`, `ObjectUrl`, `DetectedLabels` (as objects), `ThumbnailUrl`, and `UploadTimestamp`.
  - Project compiles locally (`dotnet build` succeeded).
  - Serialization test produced the expected JSON output.

- Notes: I created `tools/serialize-test` to run the test; you can delete this folder when you confirm you no longer need it.

3. Update `models/Image.cs`
3. Update `models/Image.cs`

Status: completed
Current review: The current `Image.cs` at `301397870(ricardo)_Lab#4/models/Image.cs` matches the data model and exposes these public properties:
  - `ImageId` (string)
  - `ObjectUrl` (string)
  - `DetectedLabels` (`List<Label>`) where `Label` has `Name` (string) and `Confidence` (double)
  - `ThumbNailUrl` (string)
  - `UploadTimestamp` (string)
  - `S3Bucket` (string)
  - `S3Key` (string)

Notes and mapping details:
  - The `DetectedLabels` property uses a nested `Label` class (`Name`, `Confidence`) which matches the recommended schema for storing per-label confidence values.
  - Property names use PascalCase and are ready for JSON serialization via `System.Text.Json` and for writing to DynamoDB either via PutItem (JSON) or via the DynamoDB Object Persistence Model.
  - A quick serialization test was executed (see Step 2 notes) confirming the class serializes to the expected JSON structure.

Acceptance criteria (met):
  - `models/Image.cs` exposes the required fields and a `Label` sub-type for per-label confidence.
  - Project builds and serializes the class successfully.

Optional next steps (not required):
  - Add `[DynamoDBTable("ImageMetadataTable")]` and `[DynamoDBHashKey]` attributes if you plan to use `DynamoDBContext` directly.
  - Remove the temporary `tools/serialize-test` project if you don't need it anymore.
- Acceptance criteria (met): `models/Image.cs` exposes public properties and can be serialized to JSON and written to DynamoDB. If you make optional improvements, update this tracker to reflect them.

4. Lambda for Labels (simple)

- Status: not-started
- What to do (quick path):
  1. Create a Lambda function in the Console using the `.NET 8` runtime (or use your local packaging flow).
  2. Add a trigger: S3 → ObjectCreated events for your images bucket.
  3. Minimal handler behavior: read S3 event (bucket/key), call Rekognition DetectLabels, filter by confidence (≥75%), build a JSON item and PutItem into DynamoDB.
  4. Test by uploading an image and checking CloudWatch logs.

5. Thumbnail (simpler option)

- Status: not-started
- What to do: For simplicity, implement thumbnail creation inside the same Lambda after Rekognition runs. Resize to 150px (preserve aspect ratio) and `PutObject` into `thumbnails/` prefix in same bucket. Record `ThumbnailUrl` in DynamoDB item.

6. Minimal IAM Role (Console)

- Status: not-started
- What to do: Create one IAM role via Console and attach inline policy that allows:
  - `s3:GetObject`, `s3:PutObject` on your buckets
  - `rekognition:DetectLabels`
  - `dynamodb:PutItem` on your table
  - `logs:CreateLogGroup`/`logs:CreateLogStream`/`logs:PutLogEvents`

7. (Optional) Skip Step Functions for now

- Status: not-started
- Note: Only add Step Functions if you finish the basics and want extra credit. The lab accepts using Lambda + S3 triggers.

8. Test End-to-End

- Status: not-started
- What to do: Upload a sample image, confirm:
  - Lambda executed (CloudWatch)
  - DynamoDB item created with `DetectedLabels`
  - Thumbnail present under `thumbnails/` prefix

9. Prepare Demo and Screenshots

- Status: not-started
- What to do: Record a ≤10-minute video with your camera on showing:
  - AWS Console with account ID visible
  - Uploading an image (or showing recent upload) and DynamoDB item + thumbnail
  - Do not explain code; just demonstrate the features
  - Take screenshot showing AWS account deletion confirmation when ready

10. ZIP Submission

- Status: not-started
- What to do: Zip the project source, `PROJECT_TRACKER_Lab4.md`, README, and demo video into `studentID(lastname)_Lab#4.zip` and submit.

---

## Suggested order of work (next actionable steps)
1. Create DynamoDB table in the AWS Console and record name/ARN here.
2. Update `models/Image.cs` to expose public properties matching the data model.
3. Implement and test the S3-triggered label-detection Lambda locally or in AWS (use sample images).

Short-term checklist (first work session)
- [ ] Create DynamoDB table and record details.
- [ ] Update `models/Image.cs` with public properties.
- [ ] Plan IAM role for label-detection Lambda.

---

## Notes & Helpful Commands
- Dotnet lambda packaging (example if using `Amazon.Lambda.Tools`):

```bash
dotnet lambda package --configuration Release --framework net8.0 --output-package ./artifacts/labelLambda.zip
```

- SAM deploy quick commands (after `template.yaml` is ready):

```bash
sam build
sam deploy --guided
```

---

If you'd like, I can now: add a CloudFormation/SAM template skeleton for the resources, or scaffold the Lambda handler signatures and IAM policies (I won't implement business logic unless you ask). Tell me which of those you'd like next.
