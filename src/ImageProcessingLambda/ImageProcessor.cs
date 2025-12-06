using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using _301397870_ricardo_Lab4.models;

namespace ImageProcessingLambda
{
    public class ImageProcessor
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IAmazonRekognition _rekognitionClient;
        private readonly float _confidenceThreshold;
        private readonly string _thumbPrefix;

        public ImageProcessor(IAmazonS3 s3Client, IAmazonRekognition rekognitionClient, float confidenceThreshold, string thumbPrefix)
        {
            _s3Client = s3Client;
            _rekognitionClient = rekognitionClient;
            _confidenceThreshold = confidenceThreshold;
            _thumbPrefix = thumbPrefix;
        }

        public async Task<byte[]> DownloadImageAsync(string bucket, string key)
        {
            var response = await _s3Client.GetObjectAsync(bucket, key);
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task<List<_301397870_ricardo_Lab4.models.Image.Label>> DetectLabelsAsync(byte[] imageBytes)
        {
            var request = new DetectLabelsRequest
            {
                Image = new Amazon.Rekognition.Model.Image
                {
                    Bytes = new MemoryStream(imageBytes)
                },
                MinConfidence = _confidenceThreshold
            };

            var response = await _rekognitionClient.DetectLabelsAsync(request);
            
            return response.Labels
                .Where(l => l.Confidence >= _confidenceThreshold)
                .Select(l => new _301397870_ricardo_Lab4.models.Image.Label
                {
                    Name = l.Name,
                    Confidence = (double)(l.Confidence ?? 0)
                })
                .ToList();
        }

        public async Task<string> GenerateAndUploadThumbnailAsync(string bucket, string key, byte[] imageBytes)
        {
            using var imageStream = new MemoryStream(imageBytes);
            using var img = await SixLabors.ImageSharp.Image.LoadAsync(imageStream);
            
            img.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(150, 150),
                Mode = ResizeMode.Max
            }));

            using var thumbnailStream = new MemoryStream();
            await img.SaveAsJpegAsync(thumbnailStream);
            thumbnailStream.Position = 0;

            var thumbKey = $"thumbnails/{_thumbPrefix}{Path.GetFileName(key)}";
            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucket,
                Key = thumbKey,
                InputStream = thumbnailStream
            });

            return $"s3://{bucket}/{thumbKey}";
        }
    }
}
