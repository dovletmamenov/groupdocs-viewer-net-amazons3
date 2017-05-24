using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using GroupDocs.Viewer.AmazonS3.Helpers;
using GroupDocs.Viewer.Config;
using GroupDocs.Viewer.Domain;
using GroupDocs.Viewer.Helper;

namespace GroupDocs.Viewer.AmazonS3
{
    /// <summary>
    /// The file data store for Amazon S3
    /// </summary>
    [Obsolete("Use ViewerDataHandler as a replacement.")]
    public class FileDataStore : IFileDataStore, IDisposable
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

        public FileDataStore(ViewerConfig config, IAmazonS3 client)
        {
            if (config == null)
                throw new ArgumentNullException("config");
            if (client == null)
                throw new ArgumentNullException("client");

            _config = config;
            _client = client;
        }

        /// <summary>
        /// Gets the file data.
        /// </summary>
        /// <param name="fileDescription">The file description.</param>
        /// <returns>GroupDocs.Viewer.Domain.FileData.</returns>
        public FileData GetFileData(FileDescription fileDescription)
        {
            string objectKey = GetObjectKey(fileDescription);

            GetObjectRequest request = new GetObjectRequest
            {
                Key = objectKey,
                BucketName = _bucketName,
            };

            try
            {
                using (GetObjectResponse response = _client.GetObject(request))
                    return Deserialize(response.ResponseStream);
            }
            catch (AmazonS3Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Saves the file data.
        /// </summary>
        /// <param name="fileDescription">The file description.</param>
        /// <param name="fileData">The file data.</param>
        public void SaveFileData(FileDescription fileDescription, FileData fileData)
        {
            string objectKey = GetObjectKey(fileDescription);

            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = Serialize(fileData)
            };

            _client.PutObject(request);
        }
        
        private FileData Deserialize(Stream stream)
        {
            using (XmlTextReader xmlTextReader = new XmlTextReader(stream))
            {
                XmlSerializer defaultFileDataSerializer = new XmlSerializer(typeof(FileData));
                if (defaultFileDataSerializer.CanDeserialize(xmlTextReader))
                    return defaultFileDataSerializer.Deserialize(xmlTextReader) as FileData;
            }

            return null;
        }

        private Stream Serialize(FileData fileData)
        {
            MemoryStream stream = new MemoryStream();

            XmlSerializer defaultSerializer = new XmlSerializer(typeof(FileData));
            defaultSerializer.Serialize(stream, fileData);
            return stream;
        }

        private string GetObjectKey(FileDescription fileDescription)
        {
            string fileName = Path.ChangeExtension(fileDescription.Name, "xml");
            string directoryPath =
                PathHelper.NormalizeFolderPath(fileDescription.Guid);
            string path =
                Path.Combine(_config.CachePath, directoryPath, fileName);

            return PathHelper.NormalizePath(path);
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
