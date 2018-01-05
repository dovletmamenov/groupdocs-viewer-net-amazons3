using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GroupDocs.Viewer.AmazonS3.Tests
{
    public class TestFileManager : IFileManager
    {
        public char PathDelimiter { get { return '/'; } }

        private readonly Dictionary<string, Stream> _files = new Dictionary<string, Stream>();

        public IDictionary<string, Stream> Files
        {
            get
            {
                return _files;
            }
        }

        public void Upload(Stream file, string path)
        {
            var ms = new MemoryStream();
            file.CopyTo(ms);

            ms.Position = 0;
            file.Position = 0;

            if (_files.ContainsKey(path))
            {
                _files[path] = ms;
            }
            else
            {
                _files.Add(path, ms);
            }
        }

        public Stream Download(string path)
        {
            var stream = new MemoryStream();

            _files[path].CopyTo(stream);
            _files[path].Position = 0;

            stream.Position = 0;
            return stream;
        }

        public bool FileExist(string fileName)
        {
            return _files.ContainsKey(fileName);
        }

        public IEnumerable<IFile> GetFiles(string folder)
        {
            return _files.Where(p => p.Key.StartsWith(folder))
                .Select(p => new AmazonS3File
                {
                    Path = p.Key,
                    Size = p.Value.Length
                }).ToList();
        }

        public IFile GetFile(string path)
        {
            var now = new DateTime(DateTime.Now.Year, 7, 7);

            var file = new AmazonS3File
            {
                Path = path,
                LastModified = now,
            };

            if (_files.ContainsKey(path))
                file.Size = _files[path].Length;

            return file;
        }

        public void DeleteDirectory(string path)
        {
            foreach (var entry in _files.Where(file => file.Key.StartsWith(path)).ToList())
            {
                _files[entry.Key].Close();
                _files[entry.Key].Dispose();

                _files.Remove(entry.Key);
            }
        }
    }
}