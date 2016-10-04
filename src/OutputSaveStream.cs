using System;
using System.IO;

namespace GroupDocs.Viewer.AWS.S3
{
    public class OutputSaveStream : MemoryStream
    {
        private bool _isExecuted;

        private readonly Func<Stream, bool> _executeWhenClosing;

        public OutputSaveStream(Func<Stream, bool> executeWhenClosing)
        {
            _executeWhenClosing = executeWhenClosing;
        }

        public override void Close()
        {
            if (!_isExecuted)
            {
                _isExecuted = true;
                MemoryStream stream = new MemoryStream();
                this.Position = 0;
                this.CopyTo(stream);
                stream.Position = 0;
                _executeWhenClosing(stream);
            }

            base.Close();
        }
    }
}