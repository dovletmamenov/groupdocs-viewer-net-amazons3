using System;
using System.Collections.Generic;
using System.IO;
using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;

namespace GroupDocs.Viewer.AmazonS3
{
    public class AmazonS3FileManager : IFileManager, IDisposable
    {
        private IAmazonS3 _client;

        private readonly string _bucketName;

        public AmazonS3FileManager(IAmazonS3 client, string bucketName)
        {
            _client = client;
            _bucketName = bucketName;
        }

        public char PathDelimiter
        {
            get { return '/'; }
        }

        public bool FileExist(string path)
        {
            try
            {
                GetObjectMetadataRequest request = new GetObjectMetadataRequest
                {
                    BucketName = _bucketName,
                    Key = path
                };

                _client.GetObjectMetadata(request);

                return true;
            }
            catch (AmazonS3Exception)
            {
                return false;
            }
        }

        public void Upload(Stream content, string path)
        {
            PutObjectRequest request = new PutObjectRequest
            {
                Key = path,
                BucketName = _bucketName,
                InputStream = content
            };

            _client.PutObject(request);
        }

        public Stream Download(string path)
        {
            GetObjectRequest request = new GetObjectRequest
            {
                Key = path,
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

        public IFile GetFile(string path)
        {
            GetObjectMetadataRequest request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = path
            };

            GetObjectMetadataResponse response = _client.GetObjectMetadata(request);

            IFile file = new AmazonS3File();
            file.Path = path;
            file.Size = response.ContentLength;
            file.LastModified = response.LastModified;

            return file;
        }

        public IEnumerable<IFile> GetFiles(string path)
        {
            var prefix = path.Length > 1 ? path : string.Empty;

            ListObjectsRequest request = new ListObjectsRequest
            {
                BucketName = _bucketName,
                Prefix = prefix,
                Delimiter = PathDelimiter.ToString()
            };

            ListObjectsResponse response = _client.ListObjects(request);

            List<IFile> files = new List<IFile>();
        
            // add directories 
            foreach (string directory in response.CommonPrefixes)
            {
                IFile file = new AmazonS3File();
                file.Path = directory;
                file.IsDirectory = true;

                files.Add(file);
            }

            // add files
            foreach (S3Object entry in response.S3Objects)
            {
                IFile fileDescription = new AmazonS3File()
                {
                    Path = entry.Key,
                    IsDirectory = false,
                    LastModified = entry.LastModified,
                    Size = entry.Size
                };

                files.Add(fileDescription);
            }

            return files;
        }

        public void DeleteDirectory(string path)
        {
            S3DirectoryInfo directory = new S3DirectoryInfo(_client, _bucketName, path);
            directory.Delete(true);
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }
    }
}