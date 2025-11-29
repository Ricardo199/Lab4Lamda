using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace _301397870_ricardo_Lab4.models
{
    public class Image
    {
        public string ImageId { get; set; } = string.Empty;
        public string ObjectUrl { get; set; } = string.Empty;
        public List<Label> DetectedLabels { get; set; } = new List<Label>();
        public string ThumbNailUrl { get; set; } = string.Empty;
        public string UploadTimestamp { get; set; } = string.Empty;
        public string S3Bucket { get; set; } = string.Empty;
        public string S3Key { get; set; } = string.Empty;

        public class Label
        {
            public string Name { get; set; } = string.Empty;
            public double Confidence { get; set; }
        }
    }
}