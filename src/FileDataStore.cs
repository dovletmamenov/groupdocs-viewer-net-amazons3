using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using GroupDocs.Viewer.AWS.S3.Helpers;
using GroupDocs.Viewer.Config;
using GroupDocs.Viewer.Domain;
using GroupDocs.Viewer.Helper;

namespace GroupDocs.Viewer.AWS.S3
{
    public class FileDataStore : IFileDataStore, IDisposable
    {
        private readonly ViewerConfig _config;

        private IAmazonS3 _client;

        private readonly string _bucketName;

        public FileDataStore(ViewerConfig config, IAmazonS3 client, string bucketName)
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

        private string GetObjectKey(FileDescription fileDescription)
        {
            string fileName = Path.ChangeExtension(fileDescription.Name, "xml");
            string directoryPath =
                PathHelper.ToRelativeDirectoryName(fileDescription.Guid);
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
