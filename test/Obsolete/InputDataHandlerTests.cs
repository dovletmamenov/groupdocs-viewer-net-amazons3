using System;
using System.Collections.Generic;
using System.IO;
using Amazon.S3;
using Amazon.S3.Model;
using GroupDocs.Viewer.Config;
using GroupDocs.Viewer.Domain;
using Moq;
using NUnit.Framework;

namespace GroupDocs.Viewer.AmazonS3.Tests
{
    [TestFixture]
    public class InputDataHandlerTests
    {
        private readonly ViewerConfig _viewerConfig;

        public InputDataHandlerTests()
        {
            _viewerConfig = new ViewerConfig
            {
                StoragePath = Constants.Delimiter,
                CacheFolderName = "cache"
            };
        }

        [Test]
        public void ShouldReturnFileDescriptionWhenFileExist()
        {
            DateTime lastModified = DateTime.Now.Date;
            int size = 123;

            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.GetObjectMetadata(It.IsAny<GetObjectMetadataRequest>()))
                .Returns((GetObjectMetadataRequest request) =>
                {
                    Assert.AreEqual("document.doc", request.Key);

                    return new GetObjectMetadataResponse
                    {
                        LastModified = lastModified,
                        ContentLength = size
                    };
                });

            InputDataHandler handler =
                new InputDataHandler(_viewerConfig, clientMock.Object);

            FileDescription fileDescription = handler.GetFileDescription("document.doc");

            Assert.AreEqual("document.doc", fileDescription.Guid);
            Assert.AreEqual(lastModified, fileDescription.LastModificationDate);    
            Assert.AreEqual(size, fileDescription.Size);    
        }

        [Test]
        public void ShouldReturnFileDescriptionWhenFileNotExist()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.GetObjectMetadata(It.IsAny<GetObjectMetadataRequest>()))
                .Throws(new AmazonS3Exception("") {ErrorCode = "NotFound"});

            InputDataHandler handler = new InputDataHandler(_viewerConfig, clientMock.Object);

            FileDescription fileDescription = handler.GetFileDescription("document.doc");

            Assert.AreEqual("document.doc", fileDescription.Guid);
            Assert.AreEqual(new DateTime(), fileDescription.LastModificationDate);
            Assert.AreEqual(0, fileDescription.Size);
        }

        [Test]
        public void ShouldGetFile()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.GetObject(It.IsAny<GetObjectRequest>()))
                .Returns((GetObjectRequest request) =>
                {
                    Assert.AreEqual("document.doc", request.Key);

                    MemoryStream ms = new MemoryStream();
                    ms.WriteByte(101);
                    ms.Position = 0;

                    return new GetObjectResponse { ResponseStream = ms };
                });

            InputDataHandler handler = new InputDataHandler(_viewerConfig, clientMock.Object);

            Stream stream = handler.GetFile("document.doc");
            
            Assert.AreEqual((byte)101, stream.ReadByte());
        }

        [Test]
        public void ShouldGetLastModificationDate()
        {
            DateTime lastModified = DateTime.Now.Date;

            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.GetObjectMetadata(It.IsAny<GetObjectMetadataRequest>()))
                .Returns((GetObjectMetadataRequest request) =>
                {
                    Assert.AreEqual("document.doc", request.Key);

                    return new GetObjectMetadataResponse
                    {
                        LastModified = lastModified,
                    };
                });

            InputDataHandler handler = new InputDataHandler(_viewerConfig, clientMock.Object);

            DateTime lastModificationDate = handler.GetLastModificationDate("document.doc");

            Assert.AreEqual(lastModified, lastModificationDate);
        }

        [Test, Obsolete]
        public void ShouldLoadEntities()
        {
            string storagePath = "/storage";

            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.ListObjects(It.IsAny<ListObjectsRequest>()))
                .Returns((ListObjectsRequest request) =>
                {
                    Assert.AreEqual("storage/", request.Prefix);
                    Assert.AreEqual("/", request.Delimiter);

                    ListObjectsResponse response = new ListObjectsResponse();
                    response.S3Objects.Add(new S3Object { Key = storagePath + "/document.doc" });
                    response.CommonPrefixes.Add(storagePath +  "/folder");

                    return response;
                });

            InputDataHandler handler = new InputDataHandler(_viewerConfig, clientMock.Object);

            List<FileDescription> list = handler.GetEntities(storagePath);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("/storage/document.doc", list[1].Guid);
            Assert.AreEqual(false, list[1].IsDirectory);
            Assert.AreEqual("/storage/folder", list[0].Guid);
            Assert.AreEqual(true, list[0].IsDirectory);
        }

        [Test]
        public void ShouldSaveOutputSaveStreamAndReturnInputStream()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.PutObject(It.IsAny<PutObjectRequest>()))
               .Returns((PutObjectRequest request) =>
               {
                   request.InputStream.Position = 0;

                   Assert.AreEqual("document.doc", request.Key);
                   Assert.AreEqual((byte)101, request.InputStream.ReadByte());

                   return new PutObjectResponse();
               });

            InputDataHandler handler = new InputDataHandler(_viewerConfig, clientMock.Object);

            MemoryStream documentStream = new MemoryStream();
            documentStream.WriteByte(101);

            handler.SaveDocument(new CachedDocumentDescription("document.doc"), documentStream);
        }

        [Test]
        public void ShouldAddFile()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.PutObject(It.IsAny<PutObjectRequest>()))
               .Returns((PutObjectRequest request) =>
               {
                   request.InputStream.Position = 0;

                   Assert.AreEqual("document.doc", request.Key);
                   Assert.AreEqual((byte)101, request.InputStream.ReadByte());

                   return new PutObjectResponse();
               });

            InputDataHandler handler = new InputDataHandler(_viewerConfig, clientMock.Object);

            MemoryStream documentStream = new MemoryStream();
            documentStream.WriteByte(101);

            handler.AddFile("document.doc", documentStream);
        }

        [Test]
        public void ShouldGetEntities()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.ListObjects(It.IsAny<ListObjectsRequest>()))
                .Returns((ListObjectsRequest request) =>
                {
                    Assert.AreEqual("storage/", request.Prefix);
                    Assert.AreEqual("/", request.Delimiter);

                    ListObjectsResponse response = new ListObjectsResponse();
                    response.S3Objects.Add(new S3Object { Key = "/storage/document.doc" });
                    response.CommonPrefixes.Add("/storage/folder");

                    return response;
                });

            InputDataHandler handler = new InputDataHandler(_viewerConfig, clientMock.Object);

            List<FileDescription> list = handler.GetEntities("/storage");

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("/storage/document.doc", list[1].Guid);
            Assert.AreEqual(false, list[1].IsDirectory);
            Assert.AreEqual("/storage/folder", list[0].Guid);
            Assert.AreEqual(true, list[0].IsDirectory);
        }

    }
}