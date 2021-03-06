﻿using System;
using System.Diagnostics;
using Amazon.S3;
using GroupDocs.Viewer.AmazonS3;
using GroupDocs.Viewer.Config;
using GroupDocs.Viewer.Handler;

namespace ConsoleTester
{
    static class Program
    {
        private const string Bucket = "group-docs-bucket";
        private const string FileName = "document.docx";

        static void Main(string[] args)
        {
            //TODO: 1. Set your credentials in app.config
            //TODO: 2. Set bucket name
            //TODO: 3. Upload at least one document into bucket for testing

            var amazonS3Client = new AmazonS3Client();
            var amazonS3FileManager = new AmazonS3FileManager(amazonS3Client, Bucket);
            var viewerDataHandler = new ViewerDataHandler(amazonS3FileManager);

            var viewerConfig = new ViewerConfig { EnableCaching = true };
            var handler = new ViewerHtmlHandler(viewerConfig, viewerDataHandler, viewerDataHandler);

            var pagesHtml = handler.GetPages(FileName);

            Debug.Assert(pagesHtml.Count > 0);
            Debug.Assert(!string.IsNullOrEmpty(pagesHtml[0].HtmlContent));

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }
    }
}
