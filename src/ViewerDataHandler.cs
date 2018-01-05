using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GroupDocs.Viewer.Domain;
using GroupDocs.Viewer.Handler.Cache;
using GroupDocs.Viewer.Handler.Input;

namespace GroupDocs.Viewer.AmazonS3
{
    /// <summary>
    /// GroupDocs.Viewer Input, Cache and FileData handlers implementation
    /// </summary>
    public class ViewerDataHandler : IInputDataHandler, ICacheDataHandler
    {
        private readonly IFileManager _fileManager;

        private const string PageNamePrefix = "p";
        private const string CacheFolderName = "cache";
        private const string ResourcesDirectory = "r";
        private const string AttachmentDirectory = "a";
        private const string ImageFolderNameFormat = "{0}x{1}px";
        private const string PdfFileName = "file.pdf";

        public ViewerDataHandler(IFileManager fileManager)
        {
            _fileManager = fileManager;
        }

        #region IInputDataHandler

        public void AddFile(string guid, Stream content)
        {
            var path = NormalizePath(guid);

            _fileManager.Upload(content, path);
        }

        public Stream GetFile(string guid)
        {
            var path = NormalizePath(guid);

            return _fileManager.Download(path);
        }

        public List<FileDescription> GetEntities(string path)
        {
            var normalizedPath = NormalizePath(path);
            var pathDelimiter = _fileManager.PathDelimiter.ToString();

            if (!normalizedPath.EndsWith(pathDelimiter))
                normalizedPath += pathDelimiter;

            return _fileManager.GetFiles(normalizedPath)
                .Select(_ => new FileDescription(_.Path, _.IsDirectory) { Size = _.Size })
                .ToList();
        }

        public DateTime GetLastModificationDate(string guid)
        {
            var path = NormalizePath(guid);

            var file = _fileManager.GetFile(path);

            return file.LastModified;
        }

        public FileDescription GetFileDescription(string guid)
        {
            var path = NormalizePath(guid);

            FileDescription fileDescription = new FileDescription(path);
            if (_fileManager.FileExist(path))
            {
                var entity = _fileManager.GetFile(path);

                fileDescription.Size = entity.Size;
                fileDescription.LastModificationDate = entity.LastModified;
            }

            return fileDescription;
        }

        #endregion

        #region ICacheDataHandler

        public bool Exists(CacheFileDescription cacheFileDescription)
        {
            string path = GetFilePath(cacheFileDescription);
            return _fileManager.FileExist(path);
        }

        public Stream GetInputStream(CacheFileDescription cacheFileDescription)
        {
            string path = GetFilePath(cacheFileDescription);

            return _fileManager.Download(path);
        }

        public Stream GetOutputSaveStream(CacheFileDescription cacheFileDescription)
        {
            string path = GetFilePath(cacheFileDescription);

            return new OutputStream(stream => _fileManager.Upload(stream, path));
        }

        public string GetHtmlPageResourcesFolder(CachedPageDescription cachedPageDescription)
        {
            string resourcesForPageFolderName = string.Format("{0}{1}", PageNamePrefix, cachedPageDescription.PageNumber);

            string path = cachedPageDescription.Guid.Contains(CacheFolderName)
                ? cachedPageDescription.Guid.Replace(CacheFolderName, string.Empty)
                : cachedPageDescription.Guid;

            string documentFolder = ToRelativeDirectoryName(path);
            string result = Path.Combine(CacheFolderName, documentFolder, ResourcesDirectory, resourcesForPageFolderName);

            return NormalizePath(result);
        }

        public List<CachedPageResourceDescription> GetHtmlPageResources(CachedPageDescription cachedPageDescription)
        {
            List<CachedPageResourceDescription> result = new List<CachedPageResourceDescription>();
            string resourcesFolder = GetHtmlPageResourcesFolder(cachedPageDescription);

            var pathDelimiter = _fileManager.PathDelimiter.ToString();
            if (!resourcesFolder.EndsWith(pathDelimiter))
                resourcesFolder += pathDelimiter;

            var files = _fileManager.GetFiles(resourcesFolder);

            foreach (var file in files)
            {
                if (!file.IsDirectory)
                {
                    CachedPageResourceDescription resource =
                        new CachedPageResourceDescription(cachedPageDescription, Path.GetFileName(file.Path));
                    result.Add(resource);
                }
            }

            return result;
        }

        public DateTime? GetLastModificationDate(CacheFileDescription cacheFileDescription)
        {
            string fullPath = GetFilePath(cacheFileDescription);
            var entity = _fileManager.GetFile(fullPath);

            return entity.LastModified;
        }

        public void ClearCache(TimeSpan olderThan)
        {
            _fileManager.DeleteDirectory(CacheFolderName);
        }

        public void ClearCache(string guid)
        {
            var path = NormalizePath(Path.Combine(CacheFolderName, guid));

            _fileManager.DeleteDirectory(path);
        }

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

            return NormalizePath(path);
        }

        private string GetDocumentFilePath(CacheFileDescription cacheFileDescription)
        {
            CachedDocumentDescription document = cacheFileDescription as CachedDocumentDescription;

            if (document == null)
                throw new InvalidOperationException(
                    "cacheFileDescription object should be an instance of CachedDocumentDescription class");

            string documentName = document.Name.Equals("document") && (document.OutputExtension.Equals("pdf") || document.OutputExtension.Equals(".pdf"))
                ? PdfFileName
                : Path.ChangeExtension(document.Name, document.OutputExtension);

            string documentFolder = BuildCachedDocumentFolderPath(document);
            return Path.Combine(documentFolder, documentName);
        }

        private string BuildCachedDocumentFolderPath(CachedDocumentDescription cachedPageDescription)
        {
            string path = cachedPageDescription.Guid.Contains(CacheFolderName)
                ? cachedPageDescription.Guid.Replace(CacheFolderName, string.Empty)
                : cachedPageDescription.Guid;

            string relativePath = ToRelativeDirectoryName(path);
            return Path.Combine(CacheFolderName, relativePath);
        }

        private string GetAttachmentFilePath(CacheFileDescription cacheFileDescription)
        {
            CachedAttachmentDescription attachmentDescription =
                cacheFileDescription as CachedAttachmentDescription;

            if (attachmentDescription == null)
                throw new InvalidOperationException(
                    "cacheFileDescription object should be an instance of CachedAttachmentDescription class");

            string path = attachmentDescription.Guid.Contains(CacheFolderName)
               ? attachmentDescription.Guid.Replace(CacheFolderName, string.Empty)
               : attachmentDescription.Guid;

            return Path.Combine(
               CacheFolderName,
               ToRelativeDirectoryName(path),
               AttachmentDirectory,
               attachmentDescription.AttachmentName);
        }

        private string GetResourceFilePath(CacheFileDescription cacheFileDescription)
        {
            CachedPageResourceDescription resourceDescription = cacheFileDescription as CachedPageResourceDescription;

            if (resourceDescription == null)
                throw new InvalidOperationException(
                    "cacheFileDescription object should be an instance of CachedPageResourceDescription class");

            string resourcesPath = GetHtmlPageResourcesFolder(resourceDescription.CachedPageDescription);
            return Path.Combine(resourcesPath, resourceDescription.ResourceName);
        }

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

        private string BuildPageFileName(CachedPageDescription cachedPageDescription)
        {
            var extension = cachedPageDescription.OutputExtension;

            if (string.IsNullOrEmpty(extension))
                extension = ".html";
            else if (!extension.Contains("."))
                extension = string.Format(".{0}", extension);

            return string.Format("{0}{1}{2}", PageNamePrefix, cachedPageDescription.PageNumber, extension);
        }

        private string BuildCachedPageFolderPath(CachedPageDescription cachedPageDescription)
        {
            string path = cachedPageDescription.Guid.Contains(CacheFolderName)
               ? cachedPageDescription.Guid.Replace(CacheFolderName, string.Empty)
               : cachedPageDescription.Guid;

            string relativeDirectoryName = ToRelativeDirectoryName(path);
            string dimmensionsSubFolder = GetDimmensionsSubFolder(cachedPageDescription);

            return !string.IsNullOrEmpty(dimmensionsSubFolder) 
                ? Path.Combine(CacheFolderName, relativeDirectoryName, dimmensionsSubFolder) 
                : Path.Combine(CacheFolderName, relativeDirectoryName);
        }

        private string GetDimmensionsSubFolder(CachedPageDescription cachedPageDescription)
        {
            //based on GroupDocs.Viewer.Converter.Options.ConvertImageFileType
            string outputExtension = !string.IsNullOrEmpty(cachedPageDescription.OutputExtension) && cachedPageDescription.OutputExtension.Contains(".")
                ? cachedPageDescription.OutputExtension
                : string.Format(".{0}", cachedPageDescription.OutputExtension);

            string[] possibleImageExtensions = new[] { ".jpg", ".png", ".bmp" };
            if (!possibleImageExtensions.Contains(outputExtension))
                return string.Empty;

            return string.Format(ImageFolderNameFormat, cachedPageDescription.Width, cachedPageDescription.Height);
        }

        private static string ToRelativeDirectoryName(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return string.Empty;

            string result = guid;
            const char replacementCharacter = '_';

            if (Path.IsPathRooted(result))
            {
                string root = Path.GetPathRoot(result);
                if (root.Equals(@"\"))
                    result = result.Substring(root.Length);

                if (root.Contains(":"))
                    result = result.Replace(':', replacementCharacter).Replace('\\', replacementCharacter).Replace('/', replacementCharacter);
            }

            if (result.StartsWith("http") || result.StartsWith("ftp"))
                result = result.Replace(':', replacementCharacter).Replace('\\', replacementCharacter).Replace('/', replacementCharacter);

            result = Regex.Replace(result, "[_]{2,}", new string(replacementCharacter, 1));
            result = result.TrimStart(replacementCharacter);

            return result.Replace('.', replacementCharacter);
        }

        #endregion

        #region Privates

        private string NormalizePath(string path)
        {
            return Regex.Replace(path, @"\\+", _fileManager.PathDelimiter.ToString())
                .Trim(_fileManager.PathDelimiter);
        }

        #endregion
    }
}