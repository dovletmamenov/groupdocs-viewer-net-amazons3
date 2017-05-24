using System;

namespace GroupDocs.Viewer.AmazonS3
{
    public class AmazonS3File : IFile
    {
        public string Path { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsDirectory { get; set; }
    }
}