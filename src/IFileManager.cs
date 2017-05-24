using System.Collections.Generic;
using System.IO;

namespace GroupDocs.Viewer.AmazonS3
{
    public interface IFileManager
    {
        char PathDelimiter { get; }
        bool FileExist(string path);
        void Upload(Stream content, string path);
        Stream Download(string path);
        IFile GetFile(string path);
        IEnumerable<IFile> GetFiles(string normalizedPath);
        void DeleteDirectory(string path);
    }
}