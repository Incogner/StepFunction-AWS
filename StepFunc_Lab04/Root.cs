using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace StepFunc_Lab04
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Attributes
    {
        public DateTime creationDate { get; set; }
        public string mfaAuthenticated { get; set; }
    }

    public class SessionContext
    {
        public Attributes attributes { get; set; }
    }

    public class UserIdentity
    {
        public string type { get; set; }
        public string principalId { get; set; }
        public string arn { get; set; }
        public string accountId { get; set; }
        public string accessKeyId { get; set; }
        public SessionContext sessionContext { get; set; }
    }

    public class RequestParameters
    {
        [JsonProperty("X-Amz-Date")]
        public string XAmzDate { get; set; }
        public string bucketName { get; set; }
        [JsonProperty("X-Amz-Algorithm")]
        public string XAmzAlgorithm { get; set; }
        [JsonProperty("x-amz-acl")]
        public string XAmzAcl { get; set; }
        [JsonProperty("X-Amz-SignedHeaders")]
        public string XAmzSignedHeaders { get; set; }
        public string Host { get; set; }
        [JsonProperty("X-Amz-Expires")]
        public string XAmzExpires { get; set; }
        public string key { get; set; }
        [JsonProperty("x-amz-storage-class")]
        public string XAmzStorageClass { get; set; }
    }

    public class AdditionalEventData
    {
        public string SignatureVersion { get; set; }
        public string CipherSuite { get; set; }
        public long bytesTransferredIn { get; set; }
        public string AuthenticationMethod { get; set; }
        [JsonProperty("x-amz-id-2")]
        public string XAmzId2 { get; set; }
        public long bytesTransferredOut { get; set; }
    }

    public class Resource
    {
        public string type { get; set; }
        public string ARN { get; set; }
        public string accountId { get; set; }
    }

    public class Detail
    {
        public string eventVersion { get; set; }
        public UserIdentity userIdentity { get; set; }
        public DateTime eventTime { get; set; }
        public string eventSource { get; set; }
        public string eventName { get; set; }
        public string awsRegion { get; set; }
        public string sourceIPAddress { get; set; }
        public string userAgent { get; set; }
        public RequestParameters requestParameters { get; set; }
        public object responseElements { get; set; }
        public AdditionalEventData additionalEventData { get; set; }
        public string requestID { get; set; }
        public string eventID { get; set; }
        public bool readOnly { get; set; }
        public List<Resource> resources { get; set; }
        public string eventType { get; set; }
        public bool managementEvent { get; set; }
        public string recipientAccountId { get; set; }
        public string eventCategory { get; set; }
    }

    public class Root
    {
        public string version { get; set; }
        public string id { get; set; }
        [JsonProperty("detail-type")]
        public string DetailType { get; set; }
        public string source { get; set; }
        public string account { get; set; }
        public DateTime time { get; set; }
        public string region { get; set; }
        public List<object> resources { get; set; }
        public Detail detail { get; set; }
    }


}
