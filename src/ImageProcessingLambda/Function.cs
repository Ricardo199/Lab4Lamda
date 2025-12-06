using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.Rekognition;
using Amazon.DynamoDBv2;
using _301397870_ricardo_Lab4.models;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageProcessingLambda
{
    public class Function
    {
        private readonly ImageProcessor _imageProcessor;
        private readonly DynamoDbService _dynamoDbService;

        public Function()
        {
            var s3Client = new AmazonS3Client();
            var rekognitionClient = new AmazonRekognitionClient();
            var dynamoDbClient = new AmazonDynamoDBClient();

            var tableName = Environment.GetEnvironmentVariable("IMAGE_TABLE") ?? "ImageMetadataTable";
            var confidenceThreshold = float.Parse(Environment.GetEnvironmentVariable("CONF_THRESHOLD") ?? "90");
            var thumbPrefix = Environment.GetEnvironmentVariable("THUMB_PREFIX") ?? "thumb-";

            _imageProcessor = new ImageProcessor(s3Client, rekognitionClient, confidenceThreshold, thumbPrefix);
            _dynamoDbService = new DynamoDbService(dynamoDbClient, tableName);
        }

        public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            try
            {
                var s3Key = evnt.Records[0].S3.Object.Key;

                if (s3Key.StartsWith("thumbnails/"))
                {
                    context.Logger.LogLine($"Skipping thumbnail: {s3Key}");
                    return;
                }

                var bucket = evnt.Records[0].S3.Bucket.Name;
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                var image = new _301397870_ricardo_Lab4.models.Image
                {
                    ImageId = Guid.NewGuid().ToString(),
                    S3Bucket = bucket,
                    S3Key = s3Key,
                    ObjectUrl = $"s3://{bucket}/{s3Key}",
                    UploadTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToString("o")
                };

                context.Logger.LogLine($"Processing: {s3Key}");

                var imageBytes = await _imageProcessor.DownloadImageAsync(bucket, s3Key);
                context.Logger.LogLine($"Downloaded {imageBytes.Length} bytes");

                image.DetectedLabels = await _imageProcessor.DetectLabelsAsync(imageBytes);
                context.Logger.LogLine($"Detected {image.DetectedLabels.Count} labels");

                image.ThumbNailUrl = await _imageProcessor.GenerateAndUploadThumbnailAsync(bucket, s3Key, imageBytes);
                context.Logger.LogLine($"Thumbnail: {image.ThumbNailUrl}");

                await _dynamoDbService.SaveImageMetadataAsync(image, timestamp);
                context.Logger.LogLine($"Saved to DynamoDB: {image.ImageId}");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message}");
                context.Logger.LogLine($"Stack: {ex.StackTrace}");
                throw;
            }
        }
    }
}