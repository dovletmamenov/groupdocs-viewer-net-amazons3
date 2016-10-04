using System;
using System.Collections.Generic;
using System.IO;
using Amazon.S3;
using Amazon.S3.Model;
using GroupDocs.Viewer.Config;
using GroupDocs.Viewer.Domain;
using Moq;
using NUnit.Framework;

namespace GroupDocs.Viewer.AWS.S3.Tests
{
    [TestFixture]
    public class FileDataStoreTests
    {
        private readonly ViewerConfig _viewerConfig;
        private readonly string _bucketName;

        public FileDataStoreTests()
        {
            _viewerConfig = new ViewerConfig
            {
                StoragePath = Constants.Delimiter
            };

            _bucketName = "bucket";
        }

        [Test]
        public void ShouldReturnNull()
        {
            FileDescription fileDescription =
                new FileDescription("document.doc");

            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();

            clientMock.Setup(client => client.GetObject(It.IsAny<GetObjectRequest>()))
                .Throws(new AmazonS3Exception("Error"));

            FileDataStore fileDataStore = new FileDataStore(
                _viewerConfig,
                clientMock.Object,
                _bucketName
            );

            FileData fileData = fileDataStore.GetFileData(fileDescription);

            Assert.IsNull(fileData);
        }

        [Test]
        public void ShouldSaveAndRetriveFileData()
        {
            FileData inputFileData = CreateFileData();
            MemoryStream inputStream = new MemoryStream(); 

            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.PutObject(It.IsAny<PutObjectRequest>()))
                .Returns((PutObjectRequest request) =>
                {
                    request.InputStream.Position = 0;
                    request.InputStream.CopyTo(inputStream);
                    inputStream.Position = 0;
                    return new PutObjectResponse(); 
                });
            clientMock.Setup(client => client.GetObject(It.IsAny<GetObjectRequest>()))
                .Returns((GetObjectRequest request) => new GetObjectResponse {
                    ResponseStream =  inputStream
                });

            FileDataStore fileDataStore = new FileDataStore(
                _viewerConfig,
                clientMock.Object,
                _bucketName
            );

            FileDescription fileDescription = new FileDescription("document.doc");

            fileDataStore.SaveFileData(fileDescription, inputFileData);

            FileData outputFileData = fileDataStore.GetFileData(fileDescription);

            Assert.IsNotNull(outputFileData);
            Assert.AreEqual(inputFileData.Pages.Count, outputFileData.Pages.Count);
            Assert.AreEqual(inputFileData.PageCount, outputFileData.PageCount);
            Assert.AreEqual(inputFileData.DateModified, outputFileData.DateModified);
            Assert.AreEqual(inputFileData.DateCreated, outputFileData.DateCreated);
            Assert.AreEqual(inputFileData.MaxHeight, outputFileData.MaxHeight);
            Assert.AreEqual(inputFileData.MaxWidth, outputFileData.MaxWidth);
            Assert.AreEqual(inputFileData.Pages[0].Width, outputFileData.Pages[0].Width);
            Assert.AreEqual(inputFileData.Pages[0].Height, outputFileData.Pages[0].Height);
            Assert.AreEqual(inputFileData.Pages[0].Number, outputFileData.Pages[0].Number);
        }

        private FileData CreateFileData()
        {
            DateTime now = DateTime.Now;

            FileData fileData = new FileData
            {
                DateCreated = now,
                DateModified = now,
                IsComplete = true,
                MaxHeight = 100,
                MaxWidth = 100,
                PageCount = 1
            };

            PageData page = new PageData
            {
                Width = 100,
                Height = 100,
                Number = 1,
                IsVisible = true
            };

            fileData.Pages = new List<PageData>();
            fileData.Pages.Add(page);

            return fileData;
        }
    }
}
