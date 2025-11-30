# Lab 4 - AWS Image Processing Pipeline

**Student:** Ricardo Burgos (301397870)  
**Course:** API Engineering & Cloud Comp  
**Date:** November 2025

## Overview

This project implements a serverless image processing pipeline on AWS that automatically:
- Detects labels in uploaded images using Amazon Rekognition
- Generates thumbnails (150px max dimension)
- Stores metadata in DynamoDB

## Architecture

```
S3 Bucket (amzn-s3-images-lab-4-bucket)
    ↓ (S3 Event Trigger)
Lambda Function (ImageProcessingFunction)
    ├─→ Amazon Rekognition (Label Detection, 90% confidence)
    ├─→ SixLabors.ImageSharp (Thumbnail Generation)
    └─→ DynamoDB (ImageMetadataTable)
```

## Features

- ✅ **Automatic Label Detection**: Uses AWS Rekognition to identify objects, scenes, and activities with 90%+ confidence
- ✅ **Thumbnail Generation**: Creates 150px thumbnails automatically on image upload
- ✅ **Metadata Storage**: Stores image URLs, labels, confidence scores, and timestamps in DynamoDB
- ✅ **Serverless Architecture**: No servers to manage, scales automatically

## Project Structure

```
Lab4Lamda/
├── src/
│   ├── ImageProcessingLambda/          # Lambda function code
│   │   ├── Function.cs                 # Main handler
│   │   └── ImageProcessingLambda.csproj
│   └── 301397870(ricardo)_Lab#4/       # Data models
│       └── models/
│           └── Image.cs                # Image metadata model
├── infra/
│   ├── template.yaml                   # SAM/CloudFormation template
│   ├── iam-policy.json                 # IAM policy document
│   └── README.md                       # Infrastructure documentation
├── docs/
│   ├── DOCUMENTATION.md                # Detailed project documentation
│   └── samples/                        # Test images
└── README.md                           # This file
```

## AWS Resources

- **Lambda Function**: `ImageProcessingFunction` (.NET 8, 512MB, 60s timeout)
- **S3 Bucket**: `amzn-s3-images-lab-4-bucket`
- **DynamoDB Table**: `ImageMetadataTable` (Partition Key: `ImageName`)
- **IAM Role**: `Lab4LambdaExecutionRole`

## Technology Stack

- **Runtime**: .NET 8
- **AWS Services**: Lambda, S3, Rekognition, DynamoDB
- **Libraries**:
  - Amazon.Lambda.S3Events
  - AWSSDK.S3, AWSSDK.Rekognition, AWSSDK.DynamoDBv2
  - SixLabors.ImageSharp (thumbnail generation)

## Deployment

### Prerequisites
- .NET 8 SDK
- AWS CLI configured
- Amazon.Lambda.Tools (`dotnet tool install -g Amazon.Lambda.Tools`)

### Deploy Lambda Function

```bash
cd src/ImageProcessingLambda
dotnet lambda deploy-function ImageProcessingFunction \
  --region us-east-1 \
  --function-runtime dotnet8 \
  --function-handler "ImageProcessingLambda::ImageProcessingLambda.Function::FunctionHandler" \
  --function-memory-size 512 \
  --function-timeout 60
```

### Configure S3 Trigger

```bash
# Add Lambda permission for S3
aws lambda add-permission \
  --function-name ImageProcessingFunction \
  --statement-id s3-trigger-permission \
  --action lambda:InvokeFunction \
  --principal s3.amazonaws.com \
  --source-arn arn:aws:s3:::amzn-s3-images-lab-4-bucket \
  --region us-east-1

# Configure S3 notification (replace <account-id> with your AWS account ID)
aws s3api put-bucket-notification-configuration \
  --bucket amzn-s3-images-lab-4-bucket \
  --notification-configuration '{
    "LambdaFunctionConfigurations": [{
      "LambdaFunctionArn": "arn:aws:lambda:us-east-1:<account-id>:function:ImageProcessingFunction",
      "Events": ["s3:ObjectCreated:*"]
    }]
  }' \
  --region us-east-1
```

## Usage

1. **Upload an image** to the S3 bucket:
   ```bash
   aws s3 cp your-image.jpg s3://amzn-s3-images-lab-4-bucket/
   ```

2. **Check CloudWatch Logs**:
   ```bash
   aws logs tail /aws/lambda/ImageProcessingFunction --since 5m --region us-east-1
   ```

3. **Verify thumbnail** was created:
   ```bash
   aws s3 ls s3://amzn-s3-images-lab-4-bucket/thumbnails/
   ```

4. **Query DynamoDB** for metadata:
   ```bash
   aws dynamodb scan --table-name ImageMetadataTable --region us-east-1
   ```

## Data Model

```json
{
  "ImageName": "96529d1e-77ae-4aee-9bcc-086c9a87ec2a",
  "ObjectUrl": "s3://amzn-s3-images-lab-4-bucket/test-clean.jpg",
  "S3Bucket": "amzn-s3-images-lab-4-bucket",
  "S3Key": "test-clean.jpg",
  "ThumbNailUrl": "s3://amzn-s3-images-lab-4-bucket/thumbnails/thumb-test-clean.jpg",
  "Timestamp": 1764537807759,
  "DetectedLabels": [
    {
      "Name": "Ocean",
      "Confidence": 99.8
    },
    {
      "Name": "Surfing",
      "Confidence": 95.4
    }
  ]
}
```

## Testing

The project has been successfully tested with:
- ✅ Image upload triggering Lambda execution
- ✅ Label detection with 90%+ confidence threshold
- ✅ Thumbnail generation (150px max dimension)
- ✅ DynamoDB metadata storage

Test results: **1 original image → 1 thumbnail → 1 DynamoDB entry**

## License

This project is licensed under the [GNU General Public License v3.0](https://github.com/Ricardo199/Lab4Lamda?tab=GPL-3.0-1-ov-file).

Created for educational purposes as part of the API Engineering & Cloud Comp course.

## Author

**Ricardo Burgos**  
Student ID: 301397870  
November 2025
