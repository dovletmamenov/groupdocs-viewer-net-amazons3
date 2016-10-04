using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using GroupDocs.Viewer.AWS.S3.Helpers;
using GroupDocs.Viewer.Config;
using GroupDocs.Viewer.Domain;
using GroupDocs.Viewer.Handler.Cache;

namespace GroupDocs.Viewer.AWS.S3
{
    public class CacheDataHandler : ICacheDataHandler, IDisposable
    {
        private const string AttachementDirectoryName = "attachements";

        private const string ResourcesDirecotoryName = "resources";

        private const string PageNamePrefix = "page-";

        private readonly string _bucketName;

        private readonly ViewerConfig _config;

        private IAmazonS3 _client;

        public CacheDataHandler(ViewerConfig config, IAmazonS3 client, string bucketName)
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

        public bool Exists(CacheFileDescription cacheFileDescription)
        {
            try
            {
                string key = GetObjectKey(cacheFileDescription);

                GetObjectMetadataRequest request = new GetObjectMetadataRequest()
                {
                    BucketName = _bucketName,
                    Key = key
                };

                _client.GetObjectMetadata(request);

                return true;
            }
            catch (AmazonS3Exception)
            {
                return false;
            }
        }

        public Stream GetInputStream(CacheFileDescription cacheFileDescription)
        {
            string objectKey = GetObjectKey(cacheFileDescription);

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

        public Stream GetOutputSaveStream(CacheFileDescription cacheFileDescription)
        {
            string key = GetObjectKey(cacheFileDescription);

            return new OutputSaveStream(inputStream =>
            {
                PutObjectRequest request = new PutObjectRequest()
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = inputStream
                };

                _client.PutObject(request);

                return true;
            });
        }

        public DateTime? GetLastModificationDate(CacheFileDescription cacheFileDescription)
        {
            string key = GetObjectKey(cacheFileDescription);

            GetObjectMetadataRequest request = new GetObjectMetadataRequest()
            {
                BucketName = _bucketName,
                Key = key
            };

            GetObjectMetadataResponse response = _client.GetObjectMetadata(request);

            return response.LastModified;
        }

        public string GetHtmlPageResourcesFolder(CachedPageDescription cachedPageDescription)
        {
            string resourcesForPageFolderName =
                string.Format("{0}{1}", PageNamePrefix, cachedPageDescription.PageNumber);
            string relativeDirectoryName =
                PathHelper.ToRelativeDirectoryName(cachedPageDescription.Guid);

            string path = Path.Combine(
                _config.CachePath,
                relativeDirectoryName,
                ResourcesDirecotoryName,
                resourcesForPageFolderName);

            return PathHelper.NormalizePath(path);
        }

        public List<CachedPageResourceDescription> GetHtmlPageResources(CachedPageDescription cachedPageDescription)
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

        public string GetFilePath(CacheFileDescription cacheFileDescription)
        {
            return GetObjectKey(cacheFileDescription);
        }

        public void ClearCache(TimeSpan olderThan)
        {
            DateTime now = DateTime.UtcNow;
            DateTime startFrom = now.Subtract(olderThan);

            ListObjectsRequest request = new ListObjectsRequest();
            request.BucketName = _bucketName;
            request.Marker = _config.CachePath;

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

        private string GetObjectKey(CacheFileDescription cacheFileDescription)
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

        private string BuildCachedDocumentFolderPath(CachedDocumentDescription cachedPageDescription)
        {
            string docFolder = PathHelper.ToRelativeDirectoryName(cachedPageDescription.Guid);

            return Path.Combine(_config.CachePath, docFolder);
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

            return Path.Combine(
                _config.CachePath,
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
            return Path.Combine(_config.CachePath, docFolder, dimmensionsSubFolder);
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