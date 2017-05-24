using System;
using System.IO;

namespace GroupDocs.Viewer.AmazonS3
{
    public class OutputStream : MemoryStream
    {
        private readonly Action<Stream> _onCloseAction;
        private bool _closed;

        public OutputStream(Action<Stream> onCloseAction)
        {
            _onCloseAction = onCloseAction;
        }

        public override void Close()
        {
            if (this.CanSeek && this.CanRead && !_closed)
            {
                this.Position = 0;
                this._closed = true;
                _onCloseAction(this);
            }

            base.Close();
        }
    }
}