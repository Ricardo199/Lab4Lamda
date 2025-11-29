namespace ImageProcessingLambda;

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
using System.Drawing;

public class Function
{
    private IAmazonS3 S3Client { get; set; } = new AmazonS3Client();
    private IAmazonRekognition RekognitionClient { get; set; } = new AmazonRekognitionClient();
    private IAmazonDynamoDB DynamoDbClient { get; set; } = new AmazonDynamoDBClient();
    private readonly string IMAGE_TABLE = "ImageMetadataTable";
    private readonly string CONF_THRESHOLD = "90";
    private readonly string THUMB_PREFIX = "thumb-";

    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        Image image = new()
        {
            S3Bucket = evnt.Records[0].S3.Bucket.Name,
            S3Key = evnt.Records[0].S3.Object.Key
        };
        context.Logger.LogLine($"Processing image {image.S3Key} from bucket {image.S3Bucket}");
        using (var response = await S3Client.GetObjectAsync(image.S3Bucket, image.S3Key))
        {
            using var responseStream = response.ResponseStream;
            image.ImageData = new byte[response.ContentLength];
            await responseStream.ReadAsync(image.ImageData, 0, (int)response.ContentLength);
        }
        var detectRequest = new DetectLabelsRequest
        {
            Image = new Amazon.Rekognition.Model.Image
            {
                Bytes = new MemoryStream(image.ImageData)
            },
            MinConfidence = float.Parse(CONF_THRESHOLD)
        };

        var detectResponse = await RekognitionClient.DetectLabelsAsync(detectRequest);
        context.Logger.LogLine($"Detected {detectResponse.Labels.Count} labels for image {image.S3Key}");

        image.DetectedLabels = detectResponse.Labels
            .Where(l => l.Confidence >= float.Parse(CONF_THRESHOLD)).
            Select(l => new _301397870_ricardo_Lab4.models.Image.Label
            {
                Name = l.Name,
                Confidence = l.Confidence
            }).ToList();

        using var imageStream = new MemoryStream(image.ImageData);
        using var img = await SixLabors.ImageSharp.Image.LoadAsync(imageStream);
        img.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(150, 150),
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

            image.ThumbnailUrl = $"s3://{image.S3Bucket}/{thumbKey}";
            context.Logger.LogLine($"Thumbnail saved to {image.ThumbnailUrl}");
        }
    }
}