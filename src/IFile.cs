using System;

namespace GroupDocs.Viewer.AmazonS3
{
    public interface IFile
    {
        /// <summary>
        /// Path to the file or directory in storage
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// The file size in bytes
        /// </summary>
        long Size { get; set; }

        /// <summary>
        /// The last modification date.
        /// </summary>
        DateTime LastModified { get; set; }

        /// <summary>
        /// Indicates if file is directory
        /// </summary>
        bool IsDirectory { get; set; }
    }
}