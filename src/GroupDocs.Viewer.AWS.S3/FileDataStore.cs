using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using GroupDocs.Viewer.AWS.S3.Helpers;
using GroupDocs.Viewer.Domain;
using GroupDocs.Viewer.Helper;

namespace GroupDocs.Viewer.AWS.S3
{
    /// <summary>
    /// The Amazon S3 file data store for GroupDocs.Viewer
    /// </summary>
    public class FileDataStore : IFileDataStore, IDisposable
    {
        /// <summary>
        /// The Amazon S3 client
        /// </summary>
        private IAmazonS3 _client;

        /// <summary>
        /// The Amazon S3 bucket name
        /// </summary>
        private readonly string _bucketName;

        /// <summary>
        /// The cache directory name, default is "cache"
        /// </summary>
        private const string CacheDirectoryName = "cache";

        /// <summary>
        /// Initializaes new instance of <see cref="FileDataStore"/> class.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="bucketName">The bucket name.</param>
        public FileDataStore(IAmazonS3 client, string bucketName)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            if (string.IsNullOrEmpty(bucketName))
                throw new ArgumentNullException("bucketName");

            _client = client;
            _bucketName = bucketName;
        }

        /// <summary>
        /// Retrives file data, returns null if file data does not exist
        /// </summary>
        /// <param name="fileDescription">The file description.</param>
        /// <returns>The file data or null.</returns>
        public FileData GetFileData(FileDescription fileDescription)
        {
            try
            {
                string key = GetObjectKey(fileDescription);

                using (Stream stream = _client.GetObjectStream(_bucketName, key, new Dictionary<string, object>()))
                    return Deserialize(stream);
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    amazonS3Exception.ErrorCode.Equals("NoSuchKey"))
                {
                    return null;
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
                    throw new System.Exception(string.Format("An error occurred with the message '{0}' when getting object stream.", amazonS3Exception.Message));
                }
            }
        }

        /// <summary>
        /// Saves file data for file description.
        /// </summary>
        /// <param name="fileDescription">The file description.</param>
        /// <param name="fileData">The file data.</param>
        public void SaveFileData(FileDescription fileDescription, FileData fileData)
        {
            try
            {
                string key = GetObjectKey(fileDescription);

                PutObjectRequest request = new PutObjectRequest()
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = Serialize(fileData)
                };

                _client.PutObject(request);
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
                    throw new System.Exception(string.Format("An error occurred with the message '{0}' when putting object.", amazonS3Exception.Message));
                }
            }
        }

        /// <summary>
        /// Deserialize stream into FileData object.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The file data object.</returns>
        private FileData Deserialize(Stream stream)
        {
            using (XmlTextReader xmlTextReader = new XmlTextReader(stream))
            {
                XmlSerializer wordsFileDataSerializer = new XmlSerializer(typeof(WordsFileData));
                if (wordsFileDataSerializer.CanDeserialize(xmlTextReader))
                    return wordsFileDataSerializer.Deserialize(xmlTextReader) as WordsFileData;

                XmlSerializer emailFileDataSerializer = new XmlSerializer(typeof(EmailFileData));
                if (emailFileDataSerializer.CanDeserialize(xmlTextReader))
                    return emailFileDataSerializer.Deserialize(xmlTextReader) as EmailFileData;

                XmlSerializer defaultFileDataSerializer = new XmlSerializer(typeof(FileData));
                if (defaultFileDataSerializer.CanDeserialize(xmlTextReader))
                    return defaultFileDataSerializer.Deserialize(xmlTextReader) as FileData;
            }

            return null;
        }

        /// <summary>
        /// Serialize file data objec into stream.
        /// </summary>
        /// <param name="fileData">The file data.</param>
        /// <returns>The stream.</returns>
        private Stream Serialize(FileData fileData)
        {
            MemoryStream stream = new MemoryStream();

            if (fileData is WordsFileData)
            {
                XmlSerializer wordsSerializer = new XmlSerializer(typeof(WordsFileData));
                wordsSerializer.Serialize(stream, fileData);
                return stream;
            }

            if (fileData is EmailFileData)
            {
                XmlSerializer emailSerializer = new XmlSerializer(typeof(EmailFileData));
                emailSerializer.Serialize(stream, fileData);
                return stream;
            }

            XmlSerializer defaultSerializer = new XmlSerializer(typeof(FileData));
            defaultSerializer.Serialize(stream, fileData);
            return stream;
        }

        /// <summary>
        /// Gets the object key by file description.
        /// </summary>
        /// <param name="fileDescription">The file description.</param>
        /// <returns>The object key.</returns>
        private string GetObjectKey(FileDescription fileDescription)
        {
            string fileName = Path.ChangeExtension(fileDescription.Name, "xml");
            string directoryPath =
                PathHelper.ToRelativeDirectoryName(fileDescription.Guid);
            string path = 
                Path.Combine(CacheDirectoryName, directoryPath, fileName);

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
