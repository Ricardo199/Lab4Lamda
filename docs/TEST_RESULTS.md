# Lambda Image Processing Pipeline - Test Results

**Date:** December 6, 2025  
**Tester:** Ricardo Burgos (301397870)  
**Lambda Function:** ImageProcessingFunction  
**Region:** us-east-1

## Test Summary

✅ **Status:** ALL TESTS PASSED

## Architecture Changes

### Refactored to 3-File Structure:
1. **Function.cs** - Main Lambda handler (orchestration)
2. **ImageProcessor.cs** - Image processing service (Rekognition + thumbnails)
3. **DynamoDbService.cs** - Database operations

### Benefits:
- Better separation of concerns
- Easier to test and maintain
- More modular and reusable code

## Test Results

### 1. Single Image Upload Test
- **Image:** test-1765060589.jpg (190 KB)
- **Result:** ✅ SUCCESS
- **Labels Detected:** 9 labels (Nature, Outdoors, Sea, Water, Coast, Shoreline, Aerial View, Pool, Swimming Pool)
- **Thumbnail:** Created successfully
- **DynamoDB:** Entry saved with all metadata

### 2. Bulk Upload Test
- **Images Uploaded:** 3 images
  - El-Tunco-Surf.jpg (190 KB)
  - images.jpeg (6.8 KB)
  - QHvQLhnFjrD6RgWgyZSHRn.png (2.0 MB)
- **Result:** ✅ SUCCESS
- **Processing Time:** ~8 seconds for all 3 images
- **DynamoDB Entries:** 3/3 created
- **Thumbnails:** 3/3 generated

### 3. Final Verification Test
- **Image:** final-test-1765061082.jpeg (6.8 KB)
- **Result:** ✅ SUCCESS
- **Thumbnail Path:** `thumbnails/thumb-final-test-1765061082.jpeg` (CORRECT)
- **Labels Detected:** 5 labels (Animal, Beak, Bird, Waterfowl, Anseriformes)
- **DynamoDB:** Entry saved correctly

## Current Statistics

| Metric | Count |
|--------|-------|
| Total DynamoDB Entries | 6 |
| Original Images in S3 | 5 |
| Thumbnails Generated | 5 |
| Success Rate | 100% |

## Issues Found & Fixed

### Issue #1: Stream Disposal Error
**Problem:** Lambda was failing with "Cannot access a closed Stream" error  
**Cause:** Old monolithic code had stream management issues  
**Fix:** Refactored to 3-file architecture with proper stream handling  
**Status:** ✅ RESOLVED

### Issue #2: Double Thumbnail Prefix
**Problem:** Thumbnails saved to `thumbnails/thumbnails/` instead of `thumbnails/`  
**Cause:** Environment variable `THUMB_PREFIX` was set to "thumbnails/" instead of "thumb-"  
**Fix:** Updated Lambda environment variable to `THUMB_PREFIX=thumb-`  
**Status:** ✅ RESOLVED

## Lambda Configuration

```json
{
  "Runtime": "dotnet8",
  "Handler": "ImageProcessingLambda::ImageProcessingLambda.Function::FunctionHandler",
  "Memory": "512 MB",
  "Timeout": "60 seconds",
  "Environment": {
    "IMAGE_TABLE": "ImageMetadataTable",
    "CONF_THRESHOLD": "90",
    "THUMB_PREFIX": "thumb-"
  }
}
```

## S3 Trigger Configuration

```json
{
  "Events": ["s3:ObjectCreated:*"],
  "Filter": {
    "Prefix": ""
  }
}
```

## Sample DynamoDB Entry

```json
{
  "ImageName": "0555dc25-088c-4e77-8c4f-996fb16f72ff",
  "S3Bucket": "amzn-s3-images-lab-4-bucket",
  "S3Key": "final-test-1765061082.jpeg",
  "ObjectUrl": "s3://amzn-s3-images-lab-4-bucket/final-test-1765061082.jpeg",
  "ThumbNailUrl": "s3://amzn-s3-images-lab-4-bucket/thumbnails/thumb-final-test-1765061082.jpeg",
  "Timestamp": 1765061088000,
  "DetectedLabels": [
    {"Name": "Animal", "Confidence": 98.94},
    {"Name": "Beak", "Confidence": 98.94},
    {"Name": "Bird", "Confidence": 98.94},
    {"Name": "Waterfowl", "Confidence": 92.50},
    {"Name": "Anseriformes", "Confidence": 90.52}
  ]
}
```

## Performance Metrics

- **Average Processing Time:** ~2-4 seconds per image
- **Memory Usage:** 120-156 MB (out of 512 MB allocated)
- **Cold Start:** ~400ms
- **Warm Execution:** ~2-4 seconds

## Verification Commands

### Check DynamoDB Entries
```bash
aws dynamodb scan --table-name ImageMetadataTable --region us-east-1
```

### Check Thumbnails
```bash
aws s3 ls s3://amzn-s3-images-lab-4-bucket/thumbnails/ --region us-east-1
```

### Check Lambda Logs
```bash
aws logs tail /aws/lambda/ImageProcessingFunction --since 10m --region us-east-1
```

### Upload Test Image
```bash
aws s3 cp test-image.jpg s3://amzn-s3-images-lab-4-bucket/ --region us-east-1
```

## Conclusion

The Lambda image processing pipeline is **fully functional** and successfully:
- ✅ Detects labels using Amazon Rekognition (90%+ confidence)
- ✅ Generates 150px thumbnails using SixLabors.ImageSharp
- ✅ Stores metadata in DynamoDB with proper structure
- ✅ Handles single and bulk uploads
- ✅ Skips thumbnail files to prevent infinite loops
- ✅ Uses clean 3-file architecture for maintainability

**Recommendation:** Ready for production use.

---

**Tested by:** Ricardo Burgos  
**Student ID:** 301397870  
**Course:** API Engineering & Cloud Computing  
**Date:** December 6, 2025
