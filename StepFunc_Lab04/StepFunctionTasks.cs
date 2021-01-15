using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;


using Amazon.Rekognition;
using Amazon.Rekognition.Model;

using Amazon.S3;
using Amazon.S3.Model;
using GrapeCity.Documents.Imaging;
using Newtonsoft.Json;


////
///
// Author: Mahdi Moradi - 300951014 
// 20F --API Engineering & Cloud Comp (SEC. 002) - Lab04 - AWS StepFunctions


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace StepFunc_Lab04
{
    public class StepFunctionTasks
    {
        /// <summary>
        /// The default minimum confidence used for detecting labels.
        /// </summary>
        public const float DEFAULT_MIN_CONFIDENCE = 90f;
        private readonly RegionEndpoint Region = RegionEndpoint.USEast1;

        /// <summary>
        /// The name of the environment variable to set which will override the default minimum confidence level.
        /// </summary>
        public const string MIN_CONFIDENCE_ENVIRONMENT_VARIABLE_NAME = "MinConfidence";

        IAmazonS3 S3Client { get; }
        AmazonDynamoDBClient DbClient { get; }
        DynamoDBContext DbContext { get; }

        IAmazonRekognition RekognitionClient { get; }

        float MinConfidence { get; set; } = DEFAULT_MIN_CONFIDENCE;

        HashSet<string> SupportedImageTypes { get; } = new HashSet<string> { ".png", ".jpg", ".jpeg" };

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public StepFunctionTasks()
        {
            this.S3Client = new AmazonS3Client(Region);
            this.RekognitionClient = new AmazonRekognitionClient(Region);
            this.DbClient = new AmazonDynamoDBClient(Region);
            this.DbContext = new DynamoDBContext(DbClient);

            var environmentMinConfidence = System.Environment.GetEnvironmentVariable(MIN_CONFIDENCE_ENVIRONMENT_VARIABLE_NAME);
            if (!string.IsNullOrWhiteSpace(environmentMinConfidence))
            {
                float value;
                if (float.TryParse(environmentMinConfidence, out value))
                {
                    this.MinConfidence = value;
                    Console.WriteLine($"Setting minimum confidence to {this.MinConfidence}");
                }
                else
                {
                    Console.WriteLine($"Failed to parse value {environmentMinConfidence} for minimum confidence. Reverting back to default of {this.MinConfidence}");
                }
            }
            else
            {
                Console.WriteLine($"Using default minimum confidence of {this.MinConfidence}");
            }
        }


        public MetaData ExtractImageMetaData(Root input, ILambdaContext context)
        {
            MetaData metaData = new MetaData();
            

            string bucketARN = input.detail.resources.First(x => x.type.Equals("AWS::S3::Bucket")).ARN;
            string objectARN = input.detail.resources.First(x => x.type.Equals("AWS::S3::Object")).ARN;
            string bucketName = bucketARN.Substring(bucketARN.LastIndexOf(":")+1);
            string objectKey = objectARN.Substring(objectARN.LastIndexOf("/")+1);
            Console.WriteLine($"Bucket: {bucketName}, Object: {objectKey}");

            if (!SupportedImageTypes.Contains(Path.GetExtension(objectKey)))
            {
                Console.WriteLine($"Object {bucketName}:{objectKey} is not a supported image type");
                metaData.ImageSupported = false;
            } else
            {
                // Get url Address
                GetPreSignedUrlRequest requestUrl = new GetPreSignedUrlRequest();
                requestUrl.BucketName = bucketName;
                requestUrl.Key = objectKey;
                requestUrl.Expires = DateTime.Now.Add(new TimeSpan(1, 0, 0, 0));
                string Url = S3Client.GetPreSignedURL(requestUrl);


                metaData.Url = Url.Substring(0, Url.IndexOf('?'));
                metaData.BucketName = bucketName;
                metaData.ImageName = objectKey;
                metaData.ContentType = "image/" + Path.GetExtension(objectKey).Substring(1);
                metaData.ImageSupported = true;
                
            }

            return metaData;
        }


        public async Task<MetaData> StoreImageMetaData(MetaData metaData, ILambdaContext context)
        {
            await DbContext.SaveAsync<MetaData>(metaData);
            Console.WriteLine($"Data stored in DynamoDB for {metaData.ImageName}");
            return metaData;
        }

        public async Task<MetaData> GenerateTagsAsync(MetaData metaData, ILambdaContext context)
        {
            Console.WriteLine($"Looking for labels in image {metaData.BucketName}:{metaData.ImageName}");
            var detectResponses = await this.RekognitionClient.DetectLabelsAsync(new DetectLabelsRequest
            {
                MinConfidence = MinConfidence,
                Image = new Amazon.Rekognition.Model.Image
                {
                    S3Object = new Amazon.Rekognition.Model.S3Object
                    {
                        Bucket = metaData.BucketName,
                        Name = metaData.ImageName
                    }
                }
            });

            metaData.Tags = new List<Tag>();
            foreach (var label in detectResponses.Labels)
            {
                if (metaData.Tags.Count < 10)
                {
                    Console.WriteLine($"\tFound Label {label.Name} with confidence {label.Confidence}");
                    metaData.Tags.Add(new Tag { Key = label.Name, Value = label.Confidence.ToString() });
                }
                else
                {
                    Console.WriteLine($"\tSkipped label {label.Name} with confidence {label.Confidence} because the maximum number of tags has been reached");
                }
            }

            return metaData;
        }

        public async Task<MetaData> GenerateThumbnail(MetaData metaData, ILambdaContext context)
        {

            metaData.Thumbnail = new Thumbnail
            {
                BucketName = "image-thumbnails-lab04",
                ImageName = "resized-" + metaData.ImageName,
                ContentType = metaData.ContentType
            };

            using (var response = await S3Client.GetObjectAsync(metaData.BucketName, metaData.ImageName))
            using (var responseStream = response.ResponseStream)
            using (var stream = new MemoryStream())
            {
                responseStream.CopyTo(stream);
                stream.Position = 0;
                

                GcBitmap bitmap = new GcBitmap(stream);
                var newHeight = 120;
                var newWidth = ImageUtilities.ScaleWidth((int)bitmap.Height, newHeight, (int)bitmap.Width);
                var resizedBitmap = bitmap.Resize(newWidth, newHeight, InterpolationMode.NearestNeighbor);

                var ms = new MemoryStream();
                resizedBitmap.SaveAsJpeg(ms);

                PutObjectRequest putRequest = new PutObjectRequest
                {
                    BucketName = metaData.Thumbnail.BucketName,
                    Key = metaData.Thumbnail.ImageName,
                    InputStream = ms,
                    ContentType = metaData.Thumbnail.ContentType,
                };

                // Put object
                PutObjectResponse putResponse = await S3Client.PutObjectAsync(putRequest);
            };

            // Get url Address
            GetPreSignedUrlRequest requestUrl = new GetPreSignedUrlRequest();
            requestUrl.BucketName = metaData.Thumbnail.BucketName;
            requestUrl.Key = metaData.Thumbnail.ImageName;
            requestUrl.Expires = DateTime.Now.Add(new TimeSpan(1, 0, 0, 0));
            string Url = S3Client.GetPreSignedURL(requestUrl);

            metaData.Thumbnail.Url = Url.Substring(0, Url.IndexOf('?'));

            return metaData;
        }

        public async Task AddTagsAsync(MetaData metaData, ILambdaContext context)
        {
            
            await this.S3Client.PutObjectTaggingAsync(new PutObjectTaggingRequest
            {
                BucketName = metaData.BucketName,
                Key = metaData.ImageName,
                Tagging = new Tagging
                {
                    TagSet = metaData.Tags
                }
            });
            await DbContext.SaveAsync<MetaData>(metaData);
            Console.WriteLine($"Tags Saved Successfully!");
            return;
        }

        public async Task<MetaData> StoreThumbnail(MetaData metaData, ILambdaContext context)
        {
            MetaData dbData = await DbContext.LoadAsync<MetaData>(metaData.Url);
            dbData.Thumbnail = metaData.Thumbnail;
            await DbContext.SaveAsync<MetaData>(dbData);
            Console.WriteLine($"Data stored in DynamoDB for {dbData.Thumbnail.ImageName}");

            return metaData;
        }

    }
}
