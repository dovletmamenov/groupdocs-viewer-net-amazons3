using System;
using System.IO;

namespace GroupDocs.Viewer.AmazonS3
{
    /// <summary>
    /// The output save stream 
    /// </summary>
    public class OutputSaveStream : MemoryStream
    {
        private bool _executed;

        private readonly Func<Stream, bool> _executeWhenClosing;

        public OutputSaveStream(Func<Stream, bool> executeWhenClosing)
        {
            _executeWhenClosing = executeWhenClosing;
        }

        public override void Close()
        {
            if (!_executed)
            {
                _executed = true;
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