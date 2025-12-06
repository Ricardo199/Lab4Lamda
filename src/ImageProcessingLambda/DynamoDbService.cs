using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using _301397870_ricardo_Lab4.models;

namespace ImageProcessingLambda
{
    public class DynamoDbService
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private readonly string _tableName;

        public DynamoDbService(IAmazonDynamoDB dynamoDbClient, string tableName)
        {
            _dynamoDbClient = dynamoDbClient;
            _tableName = tableName;
        }

        public async Task SaveImageMetadataAsync(_301397870_ricardo_Lab4.models.Image image, long timestamp)
        {
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

            await _dynamoDbClient.PutItemAsync(new PutItemRequest
            {
                TableName = _tableName,
                Item = item
            });
        }
    }
}
