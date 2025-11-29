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
        private IAmazonS3 S3Client { get; set; } = new AmazonS3Client();
        private IAmazonRekognition RekognitionClient { get; set; } = new AmazonRekognitionClient();
        private IAmazonDynamoDB DynamoDbClient { get; set; } = new AmazonDynamoDBClient();
        private readonly string IMAGE_TABLE = Environment.GetEnvironmentVariable("IMAGE_TABLE") ?? "ImageMetadataTable";
        private readonly string CONF_THRESHOLD = Environment.GetEnvironmentVariable("CONF_THRESHOLD") ?? "90";
        private readonly string THUMB_PREFIX = Environment.GetEnvironmentVariable("THUMB_PREFIX") ?? "thumb-";

        public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            try
            {
                var image = new Image
                {
                    ImageId = Guid.NewGuid().ToString(),
                    S3Bucket = evnt.Records[0].S3.Bucket.Name,
                    S3Key = evnt.Records[0].S3.Object.Key,
                    UploadTimestamp = DateTime.UtcNow.ToString("o")
                };
                
                context.Logger.LogLine($"Processing image {image.S3Key} from bucket {image.S3Bucket}");
                
                byte[] imageBytes;
                using (var response = await S3Client.GetObjectAsync(image.S3Bucket, image.S3Key))
                using (var memoryStream = new MemoryStream())
                {
                    await response.ResponseStream.CopyToAsync(memoryStream);
                    imageBytes = memoryStream.ToArray();
                }
                
                var detectRequest = new DetectLabelsRequest
                {
                    Image = new Amazon.Rekognition.Model.Image
                    {
                        Bytes = new MemoryStream(imageBytes)
                    },
                    MinConfidence = float.Parse(CONF_THRESHOLD)
                };

                var detectResponse = await RekognitionClient.DetectLabelsAsync(detectRequest);
                context.Logger.LogLine($"Detected {detectResponse.Labels.Count} labels for image {image.S3Key}");

                image.DetectedLabels = detectResponse.Labels
                    .Where(l => l.Confidence >= float.Parse(CONF_THRESHOLD))
                    .Select(l => new Image.Label
                    {
                        Name = l.Name,
                        Confidence = l.Confidence
                    }).ToList();

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

                        var thumbKey = $"thumbnails/{THUMB_PREFIX}{Path.GetFileName(image.S3Key)}";
                        await S3Client.PutObjectAsync(new Amazon.S3.Model.PutObjectRequest
                        {
                            BucketName = image.S3Bucket,
                            Key = thumbKey,
                            InputStream = thumbnailStream
                        });

                        image.ThumbNailUrl = $"s3://{image.S3Bucket}/{thumbKey}";
                        context.Logger.LogLine($"Thumbnail saved to {image.ThumbNailUrl}");
                    }
                }

                image.ObjectUrl = $"s3://{image.S3Bucket}/{image.S3Key}";

                var item = new Dictionary<string, AttributeValue>
                {
                    ["ImageId"] = new AttributeValue { S = image.ImageId },
                    ["ObjectUrl"] = new AttributeValue { S = image.ObjectUrl },
                    ["S3Bucket"] = new AttributeValue { S = image.S3Bucket },
                    ["S3Key"] = new AttributeValue { S = image.S3Key },
                    ["ThumbNailUrl"] = new AttributeValue { S = image.ThumbNailUrl },
                    ["UploadTimestamp"] = new AttributeValue { S = image.UploadTimestamp },
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
                context.Logger.LogLine($"Error processing image: {ex.Message}");
                context.Logger.LogLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}