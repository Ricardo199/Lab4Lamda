# Documentation Guide — Lab 4

Purpose
- Provide a clear, step-by-step guide you can follow while building and testing the solution so you produce the artifacts instructors expect for the demo and submission.
- Store artifacts in the `docs/` folder so they are easy to find and include in the ZIP.

Structure (where to save artifacts)
- `infra/` : Infrastructure as code (SAM/CloudFormation templates)
- `docs/` : All demo and documentation artifacts (this file, logs, screenshots, saved JSON, demo script)
  - `docs/commands.txt` : deployment commands + output (use `tee -a` to append)
  - `docs/screenshots/` : screenshots (dynamo_item.png, lambda_trigger.png, cloudwatch_run.png, thumbnail_in_s3.png, aws_account_deleted.png)
  - `docs/logs/` : CloudWatch log snippets (cloudwatch-<timestamp>.txt)
  - `docs/samples/` : saved DynamoDB JSON items (image-<id>.json)
  - `docs/demo-script.txt` : the exact script you will follow in the demo

Core steps (recommended order)

1) Write a short plan
- File: `docs/plan.md`
- Keep 5–10 bullets: goals, table name (`ImageMetadataTable`), S3 bucket name, Lambda name, confidence threshold, thumbnail prefix.

2) Create infra template (recommended)
- File: `infra/template.yaml` (SAM or CloudFormation)
- Resources to include: S3 bucket(s), DynamoDB table `ImageMetadataTable`, Lambda(s) (label detector + optional thumbnail), IAM roles (least privilege), and optionally a Step Functions State Machine.
- Benefit: reproducible deploy, single source of truth, easy to show during demo.

3) Create an IAM role for the Lambda(s)
- Use the Console or add to `infra/template.yaml`.
- Minimal inline policy (adjust ARNs for your account/region/buckets):

```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Effect": "Allow", "Action": ["rekognition:DetectLabels"], "Resource": "*" },
    { "Effect": "Allow", "Action": ["s3:GetObject","s3:PutObject"], "Resource": ["arn:aws:s3:::YOUR_BUCKET/*"] },
    { "Effect": "Allow", "Action": ["dynamodb:PutItem","dynamodb:UpdateItem"], "Resource": "arn:aws:dynamodb:REGION:ACCOUNT_ID:table/ImageMetadataTable" },
    { "Effect": "Allow", "Action": ["logs:CreateLogGroup","logs:CreateLogStream","logs:PutLogEvents"], "Resource": "arn:aws:logs:*:*:*" }
  ]
}
```

4) Implement the Lambda(s) locally and log carefully
- Key libraries for .NET/C# lambda: `Amazon.Lambda.S3Events`, `AWSSDK.S3`, `AWSSDK.Rekognition`, `AWSSDK.DynamoDBv2`, `ImageSharp` (thumbnailing).
- Example handler steps (high level):
  1. Parse S3 event → bucket/key
  2. Download object bytes from S3
  3. Call Rekognition DetectLabels (MinConfidence from env var)
  4. Filter labels and build `Image` object
  5. Generate thumbnail and upload to `thumbnails/` prefix (optional)
  6. Put item into DynamoDB (table `ImageMetadataTable`)
- Add logs at each step and log the final JSON written to DynamoDB.

5) Local build & package
- Build:

```bash
dotnet build
```

- Package with `dotnet lambda` or SAM. Example (Amazon.Lambda.Tools):

```bash
dotnet tool install -g Amazon.Lambda.Tools
dotnet lambda package --configuration Release --framework net8.0 --output-package ./artifacts/labelLambda.zip
```

6) Deploy and record commands
- If using SAM:

```bash
sam build
sam deploy --guided |& tee -a docs/commands.txt
```

- If using Console, record the steps and copy/paste the console output into `docs/commands.txt`.

7) Configure trigger and environment variables
- Environment variables to set on the Lambda:
  - `IMAGE_TABLE` = `ImageMetadataTable`
  - `CONF_THRESHOLD` = `75`
  - `THUMB_PREFIX` = `thumbnails/`
- Add S3 trigger: Event type `ObjectCreated` (choose prefix if needed).

8) Run test images and collect artifacts
- Upload a sample image to the input bucket (use the Console or `aws s3 cp`):

```bash
aws s3 cp sample.jpg s3://your-images-bucket/path/sample.jpg
```

- Wait for the Lambda to execute. Then capture and save:
  - CloudWatch logs for the function invocation → `docs/logs/cloudwatch-<timestamp>.txt` (copy relevant lines showing DetectLabels output and DynamoDB PutItem).
  - DynamoDB console screenshot showing the item → `docs/screenshots/dynamodb_item_<imageid>.png`.
  - Save the actual JSON that was written in `docs/samples/<imageid>.json`.
  - S3 thumbnail presence screenshot or URL → `docs/screenshots/thumbnail_<imageid>.png`.

9) Save sample JSON
- After Lambda runs, save the JSON that the Lambda wrote to DynamoDB. Example file `docs/samples/img-12345.json`.

10) Document and caption each artifact
- Create `docs/manifest.md` with a short caption per artifact (1–2 lines):
  - e.g. `docs/screenshots/dynamodb_item_img-12345.png` — "DynamoDB item created by label-lambda showing labels and timestamp."

11) Demo script (do this before recording)
- File: `docs/demo-script.txt` — a short bullet sequence and phrasing, make sure it takes ≤10 minutes.
- Example sequence:
  1. Show AWS Console with account ID in the top-right (camera on).
  2. Show S3 bucket and upload a sample image (or show recent upload list).
  3. Show CloudWatch log tail where the Lambda ran (filter by requestId).
  4. Show DynamoDB table and the new item.
  5. Show thumbnail in S3.
  6. Close with `PROJECT_TRACKER_Lab4.md` showing completed items and mention where docs are.

12) Clean-up proof (rubric requirement)
- When you delete resources (or delete account per rubric), capture a screenshot of the final confirmation and save as `docs/screenshots/aws_account_deleted.png`.

13) Prepare the submission ZIP
- Produce `studentID(lastname)_Lab#4.zip` containing:
  - `src/` (source), `PROJECT_TRACKER_Lab4.md`, `README.md`, `infra/` (if present), `docs/`, `LICENSE`.

```bash
zip -r 301397870(ricardo)_Lab#4.zip src README.md PROJECT_TRACKER_Lab4.md infra docs LICENSE
```

14) Optional: small CI check
- Add a light GitHub Actions workflow that builds the project and stores an artifact (optional). Put workflow in `.github/workflows/ci.yml` and save a screenshot of the run.

Quick checklist (artifact summary)
- `docs/plan.md`
- `infra/template.yaml` + `infra/README.md` (optional but recommended)
- `docs/commands.txt` (deployment logs)
- `docs/logs/cloudwatch-<timestamp>.txt`
- `docs/screenshots/` (DynamoDB item, Lambda trigger, CloudWatch run, thumbnail, deletion)
- `docs/samples/*.json` (saved DynamoDB item JSON)
- `docs/demo-script.txt`
- `README.md` updated with short steps and links to `docs/`

Notes & tips
- Keep the demo short and focused — show the exact artifacts listed above.
- Use descriptive file names and include timestamps in filenames when appropriate.
- If you need a template for `infra/template.yaml` or a minimal `Function.cs` scaffold, ask and I will create them for you.

---
Created for `Lab4Lamda` — save this file at `docs/DOCUMENTATION.md` so it is included in your submission ZIP.