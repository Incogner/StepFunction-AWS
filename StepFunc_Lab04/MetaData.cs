using Amazon.DynamoDBv2.DataModel;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace StepFunc_Lab04
{
    [DynamoDBTable("ImageData")]
    public class MetaData
    {
        [DynamoDBHashKey]
        public string Url { get; set; }
        public string ImageName { get; set; }
        public string BucketName { get; set; }
        public string ContentType { get; set; }
        public List<Tag> Tags { get; set; }
        public Thumbnail Thumbnail { get; set; }
        [DynamoDBIgnore]
        public bool ImageSupported { get; set; }
        
    }

    public class Thumbnail
    {
        public string Url { get; set; }
        public string ImageName { get; set; }
        public string BucketName { get; set; }
        public string ContentType { get; set; }
    }
}
