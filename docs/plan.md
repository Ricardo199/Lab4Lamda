# Lab 4 Implementation Plan

## Goals
Build an automated image processing pipeline that:
- Detects labels in images uploaded to S3 using Amazon Rekognition
- Stores metadata and detected labels (above 75% confidence) in DynamoDB
- Generates thumbnails (150px) and stores them in S3
- Demonstrates end-to-end serverless workflow using AWS Lambda

## Configuration
- DynamoDB table name: `ImageMetadataTable`
- S3 bucket name: `amzn-s3-images-lab-4-bucket`
- AWS Account ID: `599473590430`
- IAM User: `Lab4` (with AdminAccess via group `admins`)
- Lambda function name(s): 
- Confidence threshold: `75`
- Thumbnail prefix: `thumbnails/`

## High-Level Steps
1. ✅ Create DynamoDB table `ImageMetadataTable` with partition key `ImageId`
2. ✅ Create S3 bucket `amzn-s3-images-lab-4-bucket` for image storage
3. ✅ Define data model in `models/Image.cs` with required properties
10. Record demo video and capture required screenshots
11. Prepare submission ZIP with source code and documentationify DynamoDB item → verify thumbnail
10. Record demo video and capture required screenshots
11. Prepare submission ZIP with source code and documentation
- Thumbnail prefix: `thumbnails/`

