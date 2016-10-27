using GroupDocs.Viewer.AmazonS3.Helpers;
using NUnit.Framework;

namespace GroupDocs.Viewer.AmazonS3.Tests.Helpers
{
    [TestFixture]
    public class ConfigHelperTests
    {
        [Test]
        public void ShouldGetBucketNameFromConfig()
        {
            Assert.AreEqual("bucket", ConfigHelper.BucketName);
        }
    }
}