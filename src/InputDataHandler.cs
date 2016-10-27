using System;
using System.Collections.Generic;
using System.IO;
using Amazon.S3;
using Amazon.S3.Model;
using GroupDocs.Viewer.AmazonS3.Helpers;
using GroupDocs.Viewer.Config;
using GroupDocs.Viewer.Domain;
using GroupDocs.Viewer.Domain.Options;
using GroupDocs.Viewer.Handler.Input;

namespace GroupDocs.Viewer.AmazonS3
{
    /// <summary>
    /// The input data handler for Amazon S3
    /// </summary>
    public class InputDataHandler : IInputDataHandler, IDisposable
    {
        /// <summary>
        /// The GroupDocs.Viewer config
        /// </summary>
        private readonly ViewerConfig _config;

        /// <summary>
        /// The Amazon S3 client
        /// </summary>
        private IAmazonS3 _client;

        /// <summary>
        /// The Amazon S3 bucket name
        /// </summary>
        private readonly string _bucketName = ConfigHelper.BucketName;

        public InputDataHandler(ViewerConfig config, IAmazonS3 client)
        {
            if (config == null)
                throw new ArgumentNullException("config");
            if (client == null)
                throw new ArgumentNullException("client");

            _config = config;
            _client = client;
        }

        /// <summary>
        /// Gets the file description.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        /// <returns>GroupDocs.Viewer.Domain.FileDescription.</returns>
        public FileDescription GetFileDescription(string guid)
        {
            FileDescription fileDescription = new FileDescription(guid);

            try
            {
                string key = GetObjectKey(guid);

                GetObjectMetadataRequest request = new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };

                GetObjectMetadataResponse response = _client.GetObjectMetadata(request);

                fileDescription.LastModificationDate = response.LastModified;
                fileDescription.Size = response.ContentLength;

                return fileDescription;
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null && amazonS3Exception.ErrorCode.Equals("NotFound"))
                    return fileDescription;

                if (amazonS3Exception.ErrorCode != null 
                    && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    throw new System.Exception("Please check the provided AWS Credentials. " +
                                               "If you haven't signed up for Amazon S3, please visit http://aws.amazon.com/s3");
                }

                throw new System.Exception(string.Format("An error occurred with the message '{0}' when getting object metadata.", amazonS3Exception.Message));
            }
        }

        /// <summary>
        /// Retrives file from storage.
        /// </summary>
        /// <param name="guid">This is user defined key that identifies file in the storage.</param>
        /// <returns>An open stream read from to get the data from storage.</returns>
        public Stream GetFile(string guid)
        {
            string objectKey = GetObjectKey(guid);

            GetObjectRequest request = new GetObjectRequest
            {
                Key = objectKey,
                BucketName = _bucketName,
            };

            using (GetObjectResponse response = _client.GetObject(request))
            {
                MemoryStream stream = new MemoryStream();
                response.ResponseStream.CopyTo(stream);
                stream.Position = 0;

                return stream;
            }
        }

        /// <summary>
        /// Gets the file last modification date.
        /// </summary>
        /// <param name="guid">This is user defined key that identifies file in the storage.</param>
        /// <returns>The date and time the file was last modified.</returns>
        public DateTime GetLastModificationDate(string guid)
        {
            FileDescription fileDescription = GetFileDescription(guid);
            return fileDescription.LastModificationDate;
        }

        /// <summary>
        /// Loads files/folders structure for specified path
        /// </summary>
        /// <param name="fileTreeOptions">The file tree options.</param>
        /// <returns>System.Collections.Generic.List&lt;GroupDocs.Viewer.Domain.FileDescription&gt;.</returns>
        public List<FileDescription> LoadFileTree(FileTreeOptions fileTreeOptions)
        {
            var path = PathHelper.NormalizeFolderPath(fileTreeOptions.Path);

            ListObjectsRequest request = new ListObjectsRequest
            {
                BucketName = _bucketName,
                Prefix = path.Length > 1 ? path : string.Empty,
                Delimiter = Constants.Delimiter
            };

            ListObjectsResponse response = _client.ListObjects(request);

            List<FileDescription> result = new List<FileDescription>();

            // add directory objects
            foreach (string directory in response.CommonPrefixes)
            {
                FileDescription fileDescription = new FileDescription(directory, true);

                result.Add(fileDescription);
            }

            // add file objects
            foreach (S3Object entry in response.S3Objects)
            {
                FileDescription fileDescription = new FileDescription(entry.Key)
                {
                    IsDirectory = false,
                    LastModificationDate = entry.LastModified,
                    Size = entry.Size
                };

                result.Add(fileDescription);
            }

            return result;
        }

        /// <summary>
        /// Save document stream
        /// </summary>
        /// <param name="cachedDocumentDescription">Cached document description</param>
        /// <param name="documentStream">Document stream</param>
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

        private string GetObjectKey(string path)
        {
            var fullPath = path;
            if (!path.Contains(_config.StoragePath))
                fullPath = Path.Combine(_config.StoragePath, path);

            return PathHelper.NormalizePath(fullPath);
        }

        #region IDisposable
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
        #endregion
    }
}