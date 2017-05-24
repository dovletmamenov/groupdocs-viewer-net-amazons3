using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GroupDocs.Viewer.Domain;
using NUnit.Framework;

namespace GroupDocs.Viewer.AmazonS3.Tests
{
    [TestFixture]
    public class ViewerDataHandlerTests
    {
        private TestFileManager _fileManager;
        private ViewerDataHandler _viewerDataHandler;

        [SetUp]
        public void SetupFixture()
        {
            _fileManager = new TestFileManager();
            _viewerDataHandler = new ViewerDataHandler(_fileManager);
        }

        #region IInputDataHandler

        [Test]
        public void TestAddFile()
        {
            var path = "dir/file.ext";
            var stream = GetTestFileStream();

            _viewerDataHandler.AddFile(path, stream);

            Assert.IsTrue(_fileManager.Files.ContainsKey(path));
            Assert.AreEqual(3, _fileManager.Files[path].Length);
        }

        [Test]
        public void TestGetFile()
        {
            var path = "dir/file.ext";
            _fileManager.Files.Add(path, GetTestFileStream());

            var stream = _viewerDataHandler.GetFile(path);

            Assert.IsNotNull(stream);
        }

        [Test]
        public void TestGetEntities()
        {
            var path = "dir";
            _fileManager.Files.Add("dir/file1.ext", GetTestFileStream());
            _fileManager.Files.Add("dir/file2.ext", GetTestFileStream());

            var entities = _viewerDataHandler.GetEntities(path);

            Assert.AreEqual(2, entities.Count);
        }

        [Test]
        public void TestGetLastModificationDate()
        {
            var path = "dir/file.ext";
            _fileManager.Files.Add(path, GetTestFileStream());

            var date = _viewerDataHandler.GetLastModificationDate(path);

            Assert.AreEqual(DateTime.Now.Year, date.Year);
        }

        [Test]
        public void TestGetFileDescription()
        {
            var path = "dir/file.ext";
            _fileManager.Files.Add(path, GetTestFileStream());

            var fileDescription = _viewerDataHandler.GetFileDescription(path);

            Assert.AreEqual(path, fileDescription.Guid);
            Assert.AreEqual(3, fileDescription.Size);
        }

        #endregion

        #region ICacheDataHandler

        [Test]
        public void TestExistsFileExist()
        {
            var path = "cache/document_doc/p1.html";
            var stream = GetTestFileStream();
            _fileManager.Files.Add(path, stream);

            var cachedPage = new CachedPageDescription("document.doc")
            {
                PageNumber = 1,
                OutputExtension = "html"
            };

            var exists = _viewerDataHandler.Exists(cachedPage);

            Assert.IsTrue(exists);
        }

        [Test]
        public void TestExistsFileNotExist()
        {
            var path = "dir/file.ext";
            var cachedPage = new CachedPageDescription(path);

            var exists = _viewerDataHandler.Exists(cachedPage);

            Assert.IsFalse(exists);
        }

        [Test]
        public void TestGetInputStream()
        {
            var path = "cache/document_doc/p1.html";
            var stream = GetTestFileStream();
            _fileManager.Files.Add(path, stream);

            var cachedPage = new CachedPageDescription("document.doc")
            {
                PageNumber = 1,
                OutputExtension = "html"
            };

            var inputStream = _viewerDataHandler.GetInputStream(cachedPage);

            Assert.AreEqual(3, inputStream.Length);
        }

        [Test]
        public void TestGetOutputSaveStream()
        {
            var path = "cache/document_doc/p1.html";
            var stream = GetTestFileStream();

            var cachedPage = new CachedPageDescription("document.doc")
            {
                PageNumber = 1,
                OutputExtension = "html"
            };

            var outputStream = _viewerDataHandler.GetOutputSaveStream(cachedPage);

            stream.CopyTo(outputStream);
            outputStream.Close();
            outputStream.Dispose();

            Assert.AreEqual(3, _fileManager.Files[path].Length);
        }

        [Test]
        public void TestGetHtmlPageResourcesFolder()
        {
            var cachedPage = new CachedPageDescription("document.doc")
            {
                PageNumber = 1,
                OutputExtension = "html"
            };

            var folder = _viewerDataHandler.GetHtmlPageResourcesFolder(cachedPage);

            Assert.AreEqual("cache/document_doc/r/p1", folder);
        }

        [Test]
        public void TestGetHtmlPageResources()
        {
            var cachedPage = new CachedPageDescription("document.doc")
            {
                PageNumber = 1,
                OutputExtension = "html"
            };

            //resources 
            _fileManager.Files.Add("cache/document_doc/r/p1/styles.css", Stream.Null);
            _fileManager.Files.Add("cache/document_doc/r/p1/fonts.css", Stream.Null);

            var resources = _viewerDataHandler.GetHtmlPageResources(cachedPage);

            Assert.IsTrue(resources.Any(_ => _.ResourceName.Equals("styles.css")));
            Assert.IsTrue(resources.Any(_ => _.ResourceName.Equals("fonts.css")));
        }

        [Test]
        public void TestGetLastModificationDate1()
        {
            var path = "cache/p1.html";
            var cachedPage = new CachedPageDescription(path)
            {
                PageNumber = 1,
                OutputExtension = "html"
            };

            var date = _viewerDataHandler.GetLastModificationDate(cachedPage);

            Assert.IsTrue(date.HasValue);
            Assert.AreEqual(DateTime.Now.Year, date.Value.Year);
        }

        [Test]
        public void TestClearCache()
        {
            _fileManager.Files.Add("cache/p1/r/styles.css", Stream.Null);
            Assert.AreEqual(1, _fileManager.Files.Count);

            _viewerDataHandler.ClearCache(TimeSpan.MaxValue);

            Assert.AreEqual(0, _fileManager.Files.Count);
        }

        [Test]
        public void TestGetFilePath()
        {
            var path = "document.doc";
            var cachedPage = new CachedPageDescription(path)
            {
                PageNumber = 1,
                OutputExtension = "html"
            };

            var filePath = _viewerDataHandler.GetFilePath(cachedPage);

            Assert.AreEqual("cache/document_doc/p1.html", filePath);
        }

        #endregion

        #region IFileDataStore

        [Test]
        public void TestSaveGetFileData()
        {
            var fileData = CreateFileData();
            var fileDescription = new FileDescription("file.docx");

            _viewerDataHandler.SaveFileData(fileDescription, fileData);
            var retrievedFileData = _viewerDataHandler.GetFileData(fileDescription);

            Assert.AreEqual(fileData.DateCreated, retrievedFileData.DateCreated);
        }

        private FileData CreateFileData()
        {
            DateTime now = DateTime.Now;

            FileData fileData = new FileData
            {
                DateCreated = now,
                DateModified = now,
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

        #endregion

        private Stream GetTestFileStream()
        {
            return new MemoryStream(new byte[] { 100, 101, 102 });
        }
    }
}