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
- Notes: Table `ImageMetadataTable` was created in the AWS Console.
- AWS Account ID: `599473590430`
- IAM User: `Lab4` (AdminAccess permissions)
- S3 Bucket: `amzn-s3-images-lab-4-bucket` (created)
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
  

4. Lambda for Labels (simple)

- Status: completed
- What was done:
  - Created Lambda project at `src/ImageProcessingLambda`
  - Implemented `Function.cs` with FunctionHandler that:
    - Parses S3Event to get bucket and key
    - **Skips processing if image is in `thumbnails/` folder (prevents infinite loop)**
    - Downloads image from S3
    - Calls Rekognition DetectLabels with 90% confidence threshold
    - Filters labels >= 90% confidence
    - Generates 150px thumbnail using ImageSharp
    - Uploads thumbnail to `thumbnails/` prefix in S3
    - Writes complete metadata to DynamoDB `ImageMetadataTable`
  - Added all required NuGet packages (AWS SDK, Lambda, ImageSharp)
  - Referenced models project for Image class
  - Successfully packaged Lambda to `artifacts/lambda.zip` (ready for deployment)
- Acceptance criteria (met):
  - Lambda builds without errors
  - Package created: `artifacts/lambda.zip`
  - Handler signature correct: `ImageProcessingLambda::ImageProcessingLambda.Function::FunctionHandler`
  - Lambda correctly excludes thumbnail images from processing

5. Thumbnail (simpler option)

- Status: completed (integrated into Lambda function)
- Notes: Thumbnail generation implemented in same Lambda handler using ImageSharp library. Resizes to 150px max dimension, preserves aspect ratio, saves as JPEG to `thumbnails/thumb-{filename}` prefix.

6. Minimal IAM Role (Console)

- Status: completed
- What was done: 
  - Created IAM policy document in `infra/iam-policy.json` with minimal permissions
  - Created complete SAM template in `infra/template.yaml` with IAM role definition
- Permissions included:
  - `rekognition:DetectLabels`
  - `s3:GetObject`, `s3:PutObject` on bucket `amzn-s3-images-lab-4-bucket`
  - `dynamodb:PutItem`, `dynamodb:UpdateItem` on table `ImageMetadataTable`
  - CloudWatch Logs permissions
- Next: Deploy using Console or SAM CLI

7. Deploy Lambda and Configure Trigger

- Status: completed
- What was done:
  - Lambda function `ImageProcessingFunction` deployed successfully using `dotnet lambda deploy-function`
  - **Latest deployment: 2025-11-30T20:27:08 UTC**
  - Code includes thumbnail exclusion fix (prevents infinite loop)
  - Function ARN: `arn:aws:lambda:us-east-1:599473590430:function:ImageProcessingFunction`
  - Runtime: dotnet8
  - Handler: `ImageProcessingLambda::ImageProcessingLambda.Function::FunctionHandler`
  - Memory: 512MB, Timeout: 60s
  - S3 trigger configured for bucket `amzn-s3-images-lab-4-bucket`
  - Environment variables: IMAGE_TABLE, CONF_THRESHOLD, THUMB_PREFIX
- Deployment method used: AWS Lambda Tools for .NET (`dotnet lambda deploy-function`)

7. (Optional) Skip Step Functions for now

- Status: not-started
8. Test End-to-End

- Status: completed ✅
- What was done:
  1. ✅ Configured S3 trigger for Lambda function
  2. ✅ Uploaded test image: `samples/El-Tunco-Surf.jpg` → `s3://amzn-s3-images-lab-4-bucket/test-clean.jpg`
  3. ✅ Lambda executed successfully (2025-11-30T21:23:27 UTC)
  4. ✅ DynamoDB item created with detected labels (confidence >= 90%)
  5. ✅ Thumbnail generated: `thumbnails/thumb-test-clean.jpg` (7,083 bytes)
  6. ✅ **Infinite loop PREVENTED**: Second execution correctly skipped thumbnail ("Skipping thumbnail image: thumbnails/thumb-test-clean.jpg")
  7. ✅ Final result: 1 original image + 1 thumbnail + 1 DynamoDB item (no duplicates!)
- Test Results:
  - Original image: `test-clean.jpg` (194,571 bytes)
  - Thumbnail: `thumbnails/thumb-test-clean.jpg` (7,083 bytes, 150px max dimension)
  - DynamoDB Count: 1 item
  - Lambda executions: 2 (1st processed image, 2nd skipped thumbnail correctly)
- Next: Capture screenshots for demo and prepare submission

9. Prepare Demo and Screenshots

- Status: completed ✅
- What was done:
  - **6 professional screenshots captured:**
    1. ✅ DynamoDB item showing detected labels (Nature, Outdoors, Sea, Water, Coast, Shoreline with confidence scores)
    2. ✅ S3 bucket root showing original image (`test-clean.jpg`, 190 KB)
    3. ✅ S3 thumbnails folder showing generated thumbnail (`thumb-test-clean.jpg`, 6.9 KB)
    4. ✅ CloudWatch logs showing complete execution flow (Processing → Download → Label Detection → Thumbnail Upload → DynamoDB Write → Skip Thumbnail)
    5. ✅ Lambda runtime settings (`.NET 8`, 1.9 MB package, handler info)
    6. ✅ Lambda function overview with S3 trigger diagram
  - All screenshots are high quality, clearly show account ID, and demonstrate working functionality
  - Screenshots saved to `docs/screenshots/`
- Next: Record demo video
Short-term checklist (first work session)
- [x] Create DynamoDB table and record details.
- [x] Update `models/Image.cs` with public properties.
- [x] Create S3 bucket `amzn-s3-images-lab-4-bucket`.
- [x] Create folder structure and placeholder docs (`docs/plan.md`, `docs/commands.txt`, `infra/template.yaml`, etc.).
- [x] Implement Lambda handler for label detection and thumbnail generation.
- [x] Create IAM policy document (`infra/iam-policy.json`).
- [x] Create SAM template for deployment (`infra/template.yaml`).
- [x] Package Lambda function (`artifacts/lambda.zip` created successfully).
- [x] Deploy Lambda to AWS (deployed and updated).
- [x] Configure S3 trigger on Lambda (configured via AWS CLI).
- [x] Fix infinite loop issue (thumbnails/ folder exclusion working).
- [x] Test end-to-end with sample image (successful - 1 image, 1 thumbnail, 1 DB item).
- [x] Collect artifacts (6 professional screenshots captured and saved).
- [ ] Record demo video (≤10 minutes).
- [ ] Prepare submission ZIP: `301397870(Burgos)_Lab#4.zip`.

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