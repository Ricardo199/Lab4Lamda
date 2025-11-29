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
using System.Security.Cryptography.X509Certificates;

public class Function
{
    private IAmazonS3 S3Client { get; set; } = new AmazonS3Client();
    private IAmazonRekognition RekognitionClient { get; set; } = new AmazonRekognitionClient();
    private IAmazonDynamoDB DynamoDbClient { get; set; } = new AmazonDynamoDBClient();
    private string IMAGE_TABLE = "ImageMetadataTable";
    private string CONF_THRESHOLD = "90";
    private string THUMB_PREFIX = "thumb-";
    private Image image;

    [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
        async Task FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            image.S3Bucket = evnt.Records[0].S3.Bucket.Name;
            image.S3Key = evnt.Records[0].S3.Object.Key;
            context.Logger.LogLine($"Processing image {image.S3Key} from bucket {image.S3Bucket}");
            //Download object using s3 client
            using (var response = await S3Client.GetObjectAsync(image.S3Bucket, image.S3Key))
            {
                using (var responseStream = response.ResponseStream)
                {
                    image.ImageData = new byte[response.ContentLength];
                    await responseStream.ReadAsync(image.ImageData, 0, (int)response.ContentLength);
                }
            }
        }
    }
}