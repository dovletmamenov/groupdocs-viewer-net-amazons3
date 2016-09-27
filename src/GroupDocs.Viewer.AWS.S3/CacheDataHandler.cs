using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using GroupDocs.Viewer.AWS.S3.Helpers;
using GroupDocs.Viewer.Domain;
using GroupDocs.Viewer.Handler.Cache;

namespace GroupDocs.Viewer.AWS.S3
{
    /// <summary>
    /// The Amazon S3 cache data handler for GroupDocs.Viewer
    /// </summary>
    public class CacheDataHandler : ICacheDataHandler, IDisposable
    {
        /// <summary>
        /// The attachements direcotry name
        /// </summary>
        private const string AttachementDirectoryName = "attachements";

        /// <summary>
        /// The resources direcotry name
        /// </summary>
        private const string ResourcesDirecotoryName = "resources";

        /// <summary>
        /// The page name prefix
        /// </summary>
        private const string PageNamePrefix = "page-";

        /// <summary>
        /// The bucket name
        /// </summary>
        private readonly string _bucketName;

        /// <summary>
        /// The Amazon S3 client
        /// </summary>
        private IAmazonS3 _client;

        public CacheDataHandler(IAmazonS3 client, string bucketName)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullException("bucketName");

            _client = client;
            _bucketName = bucketName;
        }

        /// <summary>
        /// Existses the specified cache file description.
        /// </summary>
        /// <param name="cacheFileDescription">The cache file description.</param>
        /// <returns>System.Boolean.</returns>
        public bool Exists(CacheFileDescription cacheFileDescription)
        {
            try
            {
                string key = GetFilePath(cacheFileDescription);

                GetObjectMetadataRequest request = new GetObjectMetadataRequest()
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

        /// <summary>
        /// Get stream with cached file
        /// </summary>
        /// <param name="cacheFileDescription">The cache file description.</param>
        /// <returns>System.IO.Stream</returns>
        public Stream GetInputStream(CacheFileDescription cacheFileDescription)
        {
            try
            {
                string key = GetFilePath(cacheFileDescription);

                using (Stream stream = _client.GetObjectStream(_bucketName, key, new Dictionary<string, object>()))
                {
                    MemoryStream memoryStream = new MemoryStream();
                    stream.CopyTo(memoryStream);
                    memoryStream.Position = 0;
                    return memoryStream;
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
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

        /// <summary>
        /// Prepare stream where file will be stored
        /// </summary>
        /// <param name="cacheFileDescription">The cache file description.</param>
        /// <returns>System.IO.Stream</returns>
        public Stream GetOutputSaveStream(CacheFileDescription cacheFileDescription)
        {
            string key = GetFilePath(cacheFileDescription);

            return new OutputSaveStream(inputStream =>
            {
                try
                {
                    PutObjectRequest request = new PutObjectRequest()
                    {
                        BucketName = _bucketName,
                        Key = key,
                        InputStream = inputStream
                    };

                    _client.PutObject(request);

                    return true;
                }
                catch (AmazonS3Exception amazonS3Exception)
                {
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
            });
        }

        /// <summary>
        /// Gets the last modification date.
        /// </summary>
        /// <param name="cacheFileDescription">The cache file description.</param>
        /// <returns>System.Nullable&lt;System.DateTime&gt;.</returns>  
        public DateTime? GetLastModificationDate(CacheFileDescription cacheFileDescription)
        {
            try
            {
                string key = GetFilePath(cacheFileDescription);

                GetObjectMetadataRequest request = new GetObjectMetadataRequest()
                {
                    BucketName = _bucketName,
                    Key = key
                };

                GetObjectMetadataResponse response = _client.GetObjectMetadata(request);

                return response.LastModified;
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
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

        /// <summary>
        /// Gets the html page resources folder path.
        /// </summary>
        /// <param name="cachedPageDescription">The cached page description</param>
        /// <returns>System.String.</returns>
        public string GetHtmlPageResourcesFolder(CachedPageDescription cachedPageDescription)
        {
            string resourcesForPageFolderName = string.Format("{0}{1}", PageNamePrefix, cachedPageDescription.PageNumber);
            string relativeDirectoryName = PathHelper.ToRelativeDirectoryName(cachedPageDescription.Guid);

            string path = Path.Combine(
                Constants.CacheDirectoryName,
                relativeDirectoryName,
                ResourcesDirecotoryName,
                resourcesForPageFolderName);

            return PathHelper.NormalizePath(path);
        }

        /// <summary>
        ///  Gets the html page resources descriptions.
        /// </summary>
        /// <param name="cachedPageDescription">The cached page description</param>
        /// <returns>List of page resources descriptions</returns>
        public List<CachedPageResourceDescription> GetHtmlPageResources(CachedPageDescription cachedPageDescription)
        {
            try
            {
                string resourcesFolder = GetHtmlPageResourcesFolder(cachedPageDescription);

                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = _bucketName;
                request.Prefix = resourcesFolder;

                ListObjectsResponse response = _client.ListObjects(request);

                List<CachedPageResourceDescription> result
                    = new List<CachedPageResourceDescription>();
                foreach (S3Object entry in response.S3Objects)
                {
                    CachedPageResourceDescription resource =
                        new CachedPageResourceDescription(cachedPageDescription, Path.GetFileName(entry.Key));
                    result.Add(resource);
                }

                return result;
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
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

        /// <summary>
        /// Gets the path to the cached document.
        /// </summary>
        /// <param name="cacheFileDescription">The cached document description</param>
        /// <returns>System.String.</returns>
        public string GetFilePath(CacheFileDescription cacheFileDescription)
        {
            string path;
            switch (cacheFileDescription.CacheFileType)
            {
                case CacheFileType.Page:
                    path = GetPageFilePath(cacheFileDescription);
                    break;
                case CacheFileType.PageResource:
                    path = GetResourceFilePath(cacheFileDescription);
                    break;
                case CacheFileType.Attachment:
                    path = GetAttachmentFilePath(cacheFileDescription);
                    break;
                case CacheFileType.Document:
                    path = GetDocumentFilePath(cacheFileDescription);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return PathHelper.NormalizePath(path);
        }

        /// <summary>
        /// Clears files from cache older than specified time interval.
        /// </summary>
        /// <param name="olderThan">The time interval.</param>
        public void ClearCache(TimeSpan olderThan)
        {
            try
            {
                DateTime now = DateTime.UtcNow;
                DateTime startFrom = now.Subtract(olderThan);

                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = _bucketName;
                request.Marker = Constants.CacheDirectoryName;

                ListObjectsResponse listObjectsResponse = _client.ListObjects(request);

                List<KeyVersion> objects = listObjectsResponse.S3Objects
                    .Where(_ => _.LastModified.ToUniversalTime() < startFrom || olderThan == TimeSpan.Zero)
                    .Select(_ => new KeyVersion { Key = _.Key })
                    .ToList();

                DeleteObjectsRequest multiObjectDeleteRequest = new DeleteObjectsRequest
                {
                    BucketName = _bucketName,
                    Objects = objects
                };

                _client.DeleteObjects(multiObjectDeleteRequest);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
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

        /// <summary>
        /// Gets file path.
        /// </summary>
        /// <param name="cacheFileDescription">Cache file description.</param>
        /// <returns>File path.</returns>
        private string GetDocumentFilePath(CacheFileDescription cacheFileDescription)
        {
            CachedDocumentDescription document = cacheFileDescription as CachedDocumentDescription;

            if (document == null)
                throw new InvalidOperationException(
                    "cacheFileDescription object should be an instance of CachedDocumentDescription class");

            string documentName = Path.ChangeExtension(document.Name, document.OutputExtension);
            string documentFolder = BuildCachedDocumentFolderPath(document);
            return Path.Combine(documentFolder, documentName);
        }

        /// <summary>
        /// Builds the cache folder path.
        /// </summary>
        /// <param name="cachedPageDescription">The cache file description.</param>
        /// <returns>System.String.</returns>
        private string BuildCachedDocumentFolderPath(CachedDocumentDescription cachedPageDescription)
        {
            string docFolder = PathHelper.ToRelativeDirectoryName(cachedPageDescription.Guid);

            return Path.Combine(Constants.CacheDirectoryName, docFolder);
        }

        /// <summary>
        /// Gets the path to the cached attachment document
        /// </summary>
        /// <param name="cacheFileDescription">The cached attachment description</param>
        /// <returns>System.String</returns>
        private string GetAttachmentFilePath(CacheFileDescription cacheFileDescription)
        {
            CachedAttachmentDescription attachmentDescription = cacheFileDescription as CachedAttachmentDescription;

            if (attachmentDescription == null)
                throw new InvalidOperationException(
                    "cacheFileDescription object should be an instance of CachedAttachmentDescription class");

            string docFolder = PathHelper.ToRelativeDirectoryName(attachmentDescription.Guid);

            return Path.Combine(Constants.CacheDirectoryName,
               docFolder,
               AttachementDirectoryName,
               attachmentDescription.AttachmentName);
        }

        /// <summary>
        /// Gets resource file path.
        /// </summary>
        /// <param name="cacheFileDescription">The cached file description.</param>
        /// <returns>The resource file path.</returns>
        private string GetResourceFilePath(CacheFileDescription cacheFileDescription)
        {
            CachedPageResourceDescription resourceDescription = cacheFileDescription as CachedPageResourceDescription;

            if (resourceDescription == null)
                throw new InvalidOperationException(
                    "cacheFileDescription object should be an instance of CachedPageResourceDescription class");

            string resourcesPath = GetHtmlPageResourcesFolder(resourceDescription.CachedPageDescription);
            return Path.Combine(resourcesPath, resourceDescription.ResourceName);
        }

        /// <summary>
        /// Gets page file path.
        /// </summary>
        /// <param name="cacheFileDescription">The cached file description.</param>
        /// <returns>The page file path.</returns>
        private string GetPageFilePath(CacheFileDescription cacheFileDescription)
        {
            CachedPageDescription pageDescription = cacheFileDescription as CachedPageDescription;

            if (pageDescription == null)
                throw new InvalidOperationException(
                    "cacheFileDescription object should be an instance of CachedPageDescription class");

            string fileName = BuildPageFileName(pageDescription);
            string folder = BuildCachedPageFolderPath(pageDescription);
            return Path.Combine(folder, fileName);
        }

        /// <summary>
        /// Builds the file path for cached page.
        /// </summary>
        /// <param name="cachedPageDescription">The cache page description.</param>
        /// <returns>System.String.</returns>
        private string BuildPageFileName(CachedPageDescription cachedPageDescription)
        {
            return string.Format("{0}{1}.{2}",
                PageNamePrefix,
                cachedPageDescription.PageNumber,
                string.IsNullOrEmpty(cachedPageDescription.OutputExtension) ? "html" : cachedPageDescription.OutputExtension);
        }

        /// <summary>
        /// Builds the cache folder path.
        /// </summary>
        /// <param name="cachedPageDescription">The cache file description.</param>
        /// <returns>System.String.</returns>
        private string BuildCachedPageFolderPath(CachedPageDescription cachedPageDescription)
        {
            string docFolder = PathHelper.ToRelativeDirectoryName(cachedPageDescription.Guid);

            string dimmensionsSubFolder = GetDimmensionsSubFolder(cachedPageDescription);
            return Path.Combine(Constants.CacheDirectoryName, docFolder, dimmensionsSubFolder);
        }

        /// <summary>
        /// Gets dimmensions folder name.
        /// </summary>
        /// <param name="cachedPageDescription">The cached page description.</param>
        /// <returns>Dimmensions sub folder name e.g. 100x100px or 100x100px.pdf</returns>
        private string GetDimmensionsSubFolder(CachedPageDescription cachedPageDescription)
        {
            if (cachedPageDescription.Width == 0 && cachedPageDescription.Height == 0)
                return string.Empty;

            return string.Format("{0}x{1}px",
                cachedPageDescription.Width,
                cachedPageDescription.Height);
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