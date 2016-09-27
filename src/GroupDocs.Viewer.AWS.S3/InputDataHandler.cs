using System;
using System.Collections.Generic;
using System.IO;
using Amazon.S3;
using Amazon.S3.Model;
using GroupDocs.Viewer.AWS.S3.Helpers;
using GroupDocs.Viewer.Config;
using GroupDocs.Viewer.Domain;
using GroupDocs.Viewer.Domain.Options;
using GroupDocs.Viewer.Handler.Input;

namespace GroupDocs.Viewer.AWS.S3
{
    public class InputDataHandler : IInputDataHandler, IDisposable
    {
        private readonly ViewerConfig _config;

        private IAmazonS3 _client;

        private readonly string _bucketName;

        public InputDataHandler(ViewerConfig config, IAmazonS3 client, string bucketName)
        {
            if (config == null)
                throw new ArgumentNullException("config");
            if (client == null)
                throw new ArgumentNullException("client");
            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullException("bucketName");

            _config = config;
            _client = client;
            _bucketName = bucketName;
        }

        public FileDescription GetFileDescription(string guid)
        {
            FileDescription fileDescription = new FileDescription(guid);

            if (Exists(guid))
            {
                string objectKey = GetObjectKey(guid);

                GetObjectMetadataResponse response =
                    _client.GetObjectMetadata(_bucketName, objectKey);

                fileDescription.LastModificationDate = response.LastModified;
                fileDescription.Size = response.ContentLength;
            }

            return fileDescription;
        }

        public Stream GetFile(string guid)
        {
            string objectKey = GetObjectKey(guid);

            return _client.GetObjectStream(_bucketName, objectKey, new Dictionary<string, object>());
        }

        public DateTime GetLastModificationDate(string guid)
        {
            FileDescription fileDescription = GetFileDescription(guid);
            return fileDescription.LastModificationDate;
        }

        public List<FileDescription> LoadFileTree(FileTreeOptions fileTreeOptions)
        {
            string prefix = GetObjectKey(fileTreeOptions.Path);

            ListObjectsRequest request = new ListObjectsRequest
            {
                BucketName = _bucketName,
                Prefix = prefix
            };

            ListObjectsResponse response = _client.ListObjects(request);

            List<FileDescription> result = new List<FileDescription>();
            foreach (S3Object entry in response.S3Objects)
            {
                FileDescription fileDescription = new FileDescription(entry.Key);

                //TODO: remove storage path from begining 
                //TODO: check if is directory

                result.Add(fileDescription);
            }

            return result;
        }

        public void SaveDocument(CachedDocumentDescription cachedDocumentDescription, 
            Stream documentStream)
        {
            string objectKey = GetObjectKey(cachedDocumentDescription.Guid);

            PutObjectRequest request = new PutObjectRequest()
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = documentStream
            };

            _client.PutObject(request);
        }

        public bool Exists(string guid)
        {
            try
            {
                string key = GetObjectKey(guid);

                GetObjectMetadataRequest request = new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };

                _client.GetObjectMetadata(request);

                return true;
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    amazonS3Exception.ErrorCode.Equals("NotFound"))
                {
                    return false;
                }

                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    throw new System.Exception("Please check the provided AWS Credentials. " +
                                               "If you haven't signed up for Amazon S3, please visit http://aws.amazon.com/s3");
                }
                else
                {
                    throw new System.Exception(string.Format("An error occurred with the message '{0}' when getting object metadata.", amazonS3Exception.Message));
                }
            }
        }

        private string GetObjectKey(string guid)
        {
            if (Path.IsPathRooted(guid))
                return guid;

            string withStorage = Path.Combine(_config.StoragePath, guid);
            string path = Path.GetFullPath(withStorage);

            return PathHelper.NormalizePath(path);
        }

        /// <summary>
        /// Indicates whether Dispose was called
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Releases resources
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing && _client != null)
            {
                _client.Dispose();
                _client = null;
            }

            _disposed = true;
        }

        /// <summary>
        /// Releases resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}