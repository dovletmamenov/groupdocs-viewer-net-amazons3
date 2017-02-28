using System;
using System.IO;

namespace GroupDocs.Viewer.AmazonS3
{
    /// <summary>
    /// The output save stream 
    /// </summary>
    public class OutputSaveStream : MemoryStream
    {
        private readonly Action<Stream> _onCloseAction;

        public OutputSaveStream(Action<Stream> onCloseAction)
        {
            _onCloseAction = onCloseAction;
        }

        public override void Close()
        {
            if (this.CanSeek && this.CanRead)
            {
                this.Position = 0;
                _onCloseAction(this);
            }

            base.Close();
        }
    }
}