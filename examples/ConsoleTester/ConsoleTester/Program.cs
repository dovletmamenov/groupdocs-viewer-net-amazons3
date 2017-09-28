using System;
using System.Diagnostics;
using Amazon.S3;
using GroupDocs.Viewer.AmazonS3;
using GroupDocs.Viewer.Config;
using GroupDocs.Viewer.Handler;

namespace ConsoleTester
{
    static class Program
    {
        private const string Bucket = "your-bucket-name";
        private const string FileName = "document.docx";

        static void Main(string[] args)
        {
            //NOTES: 1. Set your credentials in app.config
            //      2. Set bucket name
            //      3. Upload at least one document to bucket for testing

            var amazonS3Client = new AmazonS3Client();
            var amazonS3FileManager = new AmazonS3FileManager(amazonS3Client, Bucket);
            var viewerDataHandler = new ViewerDataHandler(amazonS3FileManager);

            var viewerConfig = new ViewerConfig { UseCache = true };
            var handler = new ViewerHtmlHandler(viewerConfig, viewerDataHandler, viewerDataHandler);

            var pagesHtml = handler.GetPages(FileName);

            Debug.Assert(pagesHtml.Count > 0);
            Debug.Assert(!string.IsNullOrEmpty(pagesHtml[0].HtmlContent));

            Console.ReadKey();
        }
    }
}
