using System;
using System.Configuration;

namespace GroupDocs.Viewer.AmazonS3.Helpers
{
    public static class ConfigHelper
    {
        public static string BucketName
        {
            get
            {
                var bucketName =  ConfigurationManager.AppSettings["BucketName"];
                if (string.IsNullOrEmpty(bucketName))
                    throw new ApplicationException("Please add <add key=\"BucketName\" value=\"your-bucket-name\" /> to the <appSettings> config section.");

                return bucketName;
            }
        }
    }
}