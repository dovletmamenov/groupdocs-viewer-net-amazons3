using NUnit.Framework;

namespace GroupDocs.Viewer.AmazonS3.Tests
{
    [TestFixture]
    public class OutputStreamTests
    {
        [Test]
        public void ShouldCallDelegateAfterBeforeDisposed()
        {
            bool wasCalled = false;

            OutputStream outputSaveStream = new OutputStream(delegate {
                wasCalled = true;
            });

            outputSaveStream.Dispose();

            Assert.IsTrue(wasCalled);
        }

        [Test]
        public void ShouldCallDelegateAfterBeforeClosed()
        {
            bool wasCalled = false;

            OutputStream outputSaveStream = new OutputStream(delegate {
                wasCalled = true;
            });

            outputSaveStream.Close();

            Assert.IsTrue(wasCalled);
        }
    }
}