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
    public class CacheDataHandlerTests
    {
        private readonly ViewerConfig _viewerConfig;

        public CacheDataHandlerTests()
        {
            _viewerConfig = new ViewerConfig
            {
                StoragePath = Constants.Delimiter,
                CacheFolderName = "cache"
            };
        }

        [Test]
        public void ShouldReturnFilePathForCachedDocumentDescription()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();

            CacheDataHandler handler =
                new CacheDataHandler(_viewerConfig, clientMock.Object);

            string guid = "path\\document.doc";
            CachedDocumentDescription cachedDocumentDescription =
                new CachedDocumentDescription(guid)
                {
                    OutputExtension = "jpg"
                };

            string filePath = handler.GetFilePath(cachedDocumentDescription);

            Assert.AreEqual("cache/path/document.doc/document.jpg", filePath);
        }

        [Test]
        public void ShouldReturnFilePathForCachedAttachmentDescription()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();

            CacheDataHandler handler = new CacheDataHandler(_viewerConfig, clientMock.Object);

            string sourceDocumentGuid = "path\\document.doc";
            string attachementName = "attachement.zip";

            CachedAttachmentDescription cachedDocumentDescription =
                new CachedAttachmentDescription(sourceDocumentGuid, attachementName);

            string filePath = handler.GetFilePath(cachedDocumentDescription);

            Assert.AreEqual("cache/path/document.doc/attachments/attachement.zip", filePath);
        }

        [Test]
        public void ShouldReturnFilePathForCachedPageDescription()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();

            CacheDataHandler handler = new CacheDataHandler(_viewerConfig, clientMock.Object);

            string sourceDocumentGuid = "path\\document.doc";

            CachedPageDescription cachedPageDescription =
                new CachedPageDescription(sourceDocumentGuid)
                {
                    PageNumber = 1,
                    OutputExtension = "png"
                };

            string filePath = handler.GetFilePath(cachedPageDescription);

            Assert.AreEqual("cache/path/document.doc/page-1.png", filePath);
        }

        [Test]
        public void ShouldReturnFilePathForCachedPageResourceDescription()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();

            CacheDataHandler handler = new CacheDataHandler(_viewerConfig, clientMock.Object);

            string sourceDocumentGuid = "path\\document.doc";

            CachedPageDescription cachedPageDescription =
                new CachedPageDescription(sourceDocumentGuid)
                {
                    PageNumber = 1,
                    OutputExtension = "html"
                };

            CachedPageResourceDescription cachedPageResourceDescription =
                new CachedPageResourceDescription(cachedPageDescription, "styles.css");

            string filePath = handler.GetFilePath(cachedPageResourceDescription);

            Assert.AreEqual("cache/path/document.doc/resources/page-1/styles.css", filePath);
        }

        [Test]
        public void ShouldReturnTrueWhenDocumentObjectExist()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.GetObjectMetadata(It.IsAny<GetObjectMetadataRequest>()))
                .Returns((GetObjectMetadataRequest request) =>
                {
                    Assert.AreEqual("cache/document.doc/document.jpg", request.Key);
                    
                    return new GetObjectMetadataResponse();
                });

            CacheDataHandler handler = new CacheDataHandler(_viewerConfig, clientMock.Object);

            CachedDocumentDescription cachedDocumentDescription =
                  new CachedDocumentDescription("document.doc")
                  {
                      OutputExtension = "jpg"
                  };

            Assert.IsTrue(handler.Exists(cachedDocumentDescription));
        }

        [Test]
        public void ShouldReturnTrueWhenAttachementObjectExist()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.GetObjectMetadata(It.IsAny<GetObjectMetadataRequest>()))
                .Returns((GetObjectMetadataRequest request) =>
                {
                    Assert.AreEqual("cache/document.doc/attachments/attachement.zip", request.Key);

                    return new GetObjectMetadataResponse();
                });

            CacheDataHandler handler = new CacheDataHandler(_viewerConfig, clientMock.Object);

            CachedAttachmentDescription cachedDocumentDescription =
                new CachedAttachmentDescription("document.doc", "attachement.zip");

            Assert.IsTrue(handler.Exists(cachedDocumentDescription));
        }

        [Test]
        public void ShouldReturnTrueWhenPageDocumentObjectExist()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.GetObjectMetadata(It.IsAny<GetObjectMetadataRequest>()))
                 .Returns((GetObjectMetadataRequest request) =>
                 {
                     Assert.AreEqual("cache/document.doc/page-1.jpg", request.Key);

                     return new GetObjectMetadataResponse();
                 });

            CacheDataHandler handler = new CacheDataHandler(_viewerConfig, clientMock.Object);

            CachedPageDescription cachedPageDescription =
               new CachedPageDescription("document.doc")
               {
                   PageNumber = 1,
                   OutputExtension = "jpg"
               };

            Assert.IsTrue(handler.Exists(cachedPageDescription));
        }

        [Test]
        public void ShouldReturnTrueWhenPageResourceObjectExist()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.GetObjectMetadata(It.IsAny<GetObjectMetadataRequest>()))
                .Returns((GetObjectMetadataRequest request) =>
                {
                    Assert.AreEqual("cache/document.doc/resources/page-1/style.css", request.Key);

                    return new GetObjectMetadataResponse();
                });

            CacheDataHandler handler = new CacheDataHandler(_viewerConfig, clientMock.Object);

            CachedPageDescription cachedPageDescription =
                 new CachedPageDescription("document.doc")
                 {
                     PageNumber = 1,
                 };

            CachedPageResourceDescription cachedPageResourceDescription =
                new CachedPageResourceDescription(cachedPageDescription, "style.css");

            Assert.IsTrue(handler.Exists(cachedPageResourceDescription));
        }

        [Test]
        public void ShouldReturnStreamWhenDocumentObjectExist()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.GetObject(It.IsAny<GetObjectRequest>()))
                .Returns((GetObjectRequest request) =>
                {
                    Assert.AreEqual("cache/document.doc/document.css", request.Key);

                    MemoryStream ms = new MemoryStream();
                    ms.WriteByte(101);
                    ms.Position = 0;

                    return new GetObjectResponse { ResponseStream = ms };
                });

            CacheDataHandler handler =
                new CacheDataHandler(_viewerConfig, clientMock.Object);

            CachedDocumentDescription cachedDocumentDescription =
                  new CachedDocumentDescription("document.doc")
                  {
                      OutputExtension = "css"
                  };

            Stream stream = handler.GetInputStream(cachedDocumentDescription);

            Assert.IsNotNull(stream);
            Assert.AreEqual((byte)101, stream.ReadByte());
        }

        [Test]
        public void ShouldSaveOutputSaveStreamAndReturnInputStream()
        {
            MemoryStream tempStream = new MemoryStream();

            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.GetObject(It.IsAny<GetObjectRequest>()))
                .Returns((GetObjectRequest request) =>
                {
                    Assert.AreEqual("cache/document.doc/document.css", request.Key);

                    return new GetObjectResponse { ResponseStream = tempStream };
                });
            clientMock.Setup(client => client.PutObject(It.IsAny<PutObjectRequest>()))
               .Returns((PutObjectRequest request) =>
               {
                   Assert.AreEqual("cache/document.doc/document.css", request.Key);
                   Assert.AreEqual((byte)101, request.InputStream.ReadByte());
                   request.InputStream.Position = 0;

                   request.InputStream.CopyTo(tempStream);
                   tempStream.Position = 0;

                   return new PutObjectResponse();
               });

            CacheDataHandler handler =
                new CacheDataHandler(_viewerConfig, clientMock.Object);

            CachedDocumentDescription cachedDocumentDescription =
                  new CachedDocumentDescription("document.doc")
                  {
                      OutputExtension = "css"
                  };

            using (Stream outputStream = handler.GetOutputSaveStream(cachedDocumentDescription))
            {
                outputStream.WriteByte(101);
            }

            Stream inputStream = handler.GetInputStream(cachedDocumentDescription);

            Assert.IsNotNull(inputStream);
            Assert.AreEqual(0, inputStream.Position);
            Assert.AreEqual((byte)101, inputStream.ReadByte());
        }

        [Test]
        public void ShouldReturnLastModificationDate()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.GetObjectMetadata(It.IsAny<GetObjectMetadataRequest>()))
                .Returns((GetObjectMetadataRequest request) =>
                {
                    Assert.AreEqual("cache/document.doc/document.css", request.Key);

                    return new GetObjectMetadataResponse() { LastModified = DateTime.Now };
                });

            CacheDataHandler handler =
                new CacheDataHandler(_viewerConfig, clientMock.Object);

            CachedDocumentDescription cachedDocumentDescription =
                  new CachedDocumentDescription("document.doc")
                  {
                      OutputExtension = "css"
                  };

            Assert.IsNotNull(handler.GetLastModificationDate(cachedDocumentDescription));
        }

        [Test]
        public void ShouldReturnHtmlPageResourcesFolder()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();

            CacheDataHandler handler =
                new CacheDataHandler(_viewerConfig, clientMock.Object);

            CachedPageDescription cachedPageDescription =
                new CachedPageDescription("document.doc")
                {
                    PageNumber = 1,
                    OutputExtension = "png"
                };

            string htmlPageResourceFolder = handler.GetHtmlPageResourcesFolder(cachedPageDescription);

            Assert.AreEqual("cache/document.doc/resources/page-1/", htmlPageResourceFolder);
        }

        [Test]
        public void ShouldReturnHtmlPageResources()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.ListObjects(It.IsAny<ListObjectsRequest>()))
              .Returns((ListObjectsRequest request) =>
              {
                  Assert.AreEqual("cache/document.doc/resources/page-1/", request.Prefix);

                  var s3Objects = new List<S3Object>
                  {
                      new S3Object { Key = "cache/document.doc/resources/page-1/document.css" }
                  };
                  return new ListObjectsResponse() { S3Objects = s3Objects };
              });

            CacheDataHandler handler =
                new CacheDataHandler(_viewerConfig, clientMock.Object);

            CachedPageDescription cachedPageDescription =
                 new CachedPageDescription("document.doc")
                 {
                     PageNumber = 1
                 };

            List<CachedPageResourceDescription> htmlPageResources =
                handler.GetHtmlPageResources(cachedPageDescription);

            Assert.AreEqual(1, htmlPageResources.Count);
            Assert.AreEqual("document.doc", htmlPageResources[0].CachedPageDescription.Guid);
            Assert.AreEqual("document.css", htmlPageResources[0].ResourceName);
        }

        [Test]
        public void ShouldClearCache()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.ListObjects(It.IsAny<ListObjectsRequest>()))
             .Returns((ListObjectsRequest request) =>             {
                 var s3Objects = new List<S3Object>
                  {
                      new S3Object { Key = "document.doc", LastModified = DateTime.Now }
                  };
                 return new ListObjectsResponse { S3Objects = s3Objects };
             });
            clientMock.Setup(client => client.DeleteObjects(It.IsAny<DeleteObjectsRequest>()))
             .Returns((DeleteObjectsRequest request) => {
                 Assert.AreEqual("document.doc", request.Objects[0].Key);

                 return new DeleteObjectsResponse();
             });

            CacheDataHandler handler =
                new CacheDataHandler(_viewerConfig, clientMock.Object);

            handler.ClearCache(TimeSpan.Zero);
        }

        [Test]
        public void ShouldNotClearCache()
        {
            Mock<IAmazonS3> clientMock = new Mock<IAmazonS3>();
            clientMock.Setup(client => client.ListObjects(It.IsAny<ListObjectsRequest>()))
             .Returns((ListObjectsRequest request) => {
                 Assert.AreEqual(@"/cache", request.Marker);

                 var s3Objects = new List<S3Object>
                  {
                      new S3Object { Key = "document.doc", LastModified = DateTime.Now.AddHours(1) }
                  };
                 return new ListObjectsResponse { S3Objects = s3Objects };
             });
            clientMock.Setup(client => client.DeleteObjects(It.IsAny<DeleteObjectsRequest>()))
             .Returns((DeleteObjectsRequest request) => {
                 Assert.AreEqual(0, request.Objects.Count);
               
                 return new DeleteObjectsResponse();
             });

            CacheDataHandler handler =
                new CacheDataHandler(_viewerConfig, clientMock.Object);

            handler.ClearCache(TimeSpan.FromHours(0.5));
        }
    }
}