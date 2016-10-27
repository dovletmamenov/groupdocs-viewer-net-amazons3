﻿using System.IO;
using NUnit.Framework;

namespace GroupDocs.Viewer.AmazonS3.Tests
{
    [TestFixture]
    public class OutputSaveStreamTests
    {
        [Test]
        public void ShouldCallDelegateAfterBeforeDisposed()
        {
            bool wasCalled = false;

            OutputSaveStream outputSaveStream = new OutputSaveStream(delegate(Stream stream)
            {
                wasCalled = true;
                return true;
            });

            outputSaveStream.Dispose();

            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void ShouldCallDelegateAfterBeforeClosed()
        {
            bool wasCalled = false;

            OutputSaveStream outputSaveStream = new OutputSaveStream(delegate (Stream stream)
            {
                wasCalled = true;
                return true;
            });

            outputSaveStream.Close();

            Assert.IsTrue(wasCalled);
        }
    }
}