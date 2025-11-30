using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using _301397870_ricardo_Lab4.models;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ImageProcessingLambda
{
    public class Function
    {
        private readonly IAmazonS3 S3Client = new AmazonS3Client();
        private readonly AmazonRekognitionClient RekognitionClient = new AmazonRekognitionClient();
        private readonly AmazonDynamoDBClient DynamoDbClient = new AmazonDynamoDBClient();

        private readonly string IMAGE_TABLE = Environment.GetEnvironmentVariable("IMAGE_TABLE") ?? "ImageMetadataTable";
        private readonly string CONF_THRESHOLD = Environment.GetEnvironmentVariable("CONF_THRESHOLD") ?? "90";
        private readonly string THUMB_PREFIX = Environment.GetEnvironmentVariable("THUMB_PREFIX") ?? "thumb-";

        public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            try
            {
                var s3Key = evnt.Records[0].S3.Object.Key;

                if(s3Key.StartsWith("thumbnails/"))
                {
                    context.Logger.LogLine($"Skipping thumbnail image: {s3Key}");
                    return;
                }
                var image = new _301397870_ricardo_Lab4.models.Image();
                image.S3Bucket = evnt.Records[0].S3.Bucket.Name;
                image.S3Key = evnt.Records[0].S3.Object.Key;
                image.ImageId = Guid.NewGuid().ToString();
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                image.UploadTimestamp = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToString("o");
                image.ObjectUrl = $"s3://{image.S3Bucket}/{image.S3Key}";

                context.Logger.LogLine($"Processing image: {image.S3Key}");

                // Download image from S3
                var getObjectResponse = await S3Client.GetObjectAsync(image.S3Bucket, image.S3Key);
                byte[] imageBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await getObjectResponse.ResponseStream.CopyToAsync(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }

                context.Logger.LogLine($"Downloaded {imageBytes.Length} bytes");

                // Detect labels using Rekognition
                var detectRequest = new DetectLabelsRequest
                {
                    Image = new Amazon.Rekognition.Model.Image
                    {
                        Bytes = new MemoryStream(imageBytes)
                    },
                    MinConfidence = float.Parse(CONF_THRESHOLD)
                };

                var detectResponse = await RekognitionClient.DetectLabelsAsync(detectRequest);
                context.Logger.LogLine($"Detected {detectResponse.Labels.Count} labels");

                // Filter and store labels
                image.DetectedLabels = detectResponse.Labels
                    .Where(l => l.Confidence >= float.Parse(CONF_THRESHOLD))
                    .Select(l => new _301397870_ricardo_Lab4.models.Image.Label
                    {
                        Name = l.Name,
                        Confidence = (double)(l.Confidence ?? 0)  
                    })
                    .ToList();

                // Generate thumbnail
                using (var imageStream = new MemoryStream(imageBytes))
                using (var img = await SixLabors.ImageSharp.Image.LoadAsync(imageStream))
                {
                    img.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new SixLabors.ImageSharp.Size(150, 150),
                        Mode = ResizeMode.Max
                    }));

                    using (var thumbnailStream = new MemoryStream())
                    {
                        await img.SaveAsJpegAsync(thumbnailStream);
                        thumbnailStream.Position = 0;

                        // Upload thumbnail
                        var thumbKey = $"thumbnails/{THUMB_PREFIX}{Path.GetFileName(image.S3Key)}";
                        await S3Client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
                        {
                            BucketName = image.S3Bucket,
                            Key = thumbKey,
                            InputStream = thumbnailStream
                        });

                        image.ThumbNailUrl = $"s3://{image.S3Bucket}/{thumbKey}";
                        context.Logger.LogLine($"Uploaded thumbnail to {thumbKey}");
                    }
                }

                // Write to DynamoDB
                var item = new Dictionary<string, AttributeValue>
                {
                    ["ImageName"] = new AttributeValue { S = image.ImageId },
                    ["Timestamp"] = new AttributeValue { N = timestamp.ToString() },
                    ["ObjectUrl"] = new AttributeValue { S = image.ObjectUrl },
                    ["S3Bucket"] = new AttributeValue { S = image.S3Bucket },
                    ["S3Key"] = new AttributeValue { S = image.S3Key },
                    ["ThumbNailUrl"] = new AttributeValue { S = image.ThumbNailUrl },
                    ["DetectedLabels"] = new AttributeValue
                    {
                        L = image.DetectedLabels.Select(l => new AttributeValue
                        {
                            M = new Dictionary<string, AttributeValue>
                            {
                                ["Name"] = new AttributeValue { S = l.Name },
                                ["Confidence"] = new AttributeValue { N = l.Confidence.ToString() }
                            }
                        }).ToList()
                    }
                };

                await DynamoDbClient.PutItemAsync(new PutItemRequest
                {
                    TableName = IMAGE_TABLE,
                    Item = item
                });

                context.Logger.LogLine($"Wrote item to DynamoDB: {image.ImageId}");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message}");
                context.Logger.LogLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}