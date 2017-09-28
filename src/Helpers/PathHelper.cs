using System.Text.RegularExpressions;

namespace GroupDocs.Viewer.AmazonS3.Helpers
{
    internal static class PathHelper
    {
        public static string NormalizePath(string path)
        {
            return Regex.Replace(path, @"\\+", Constants.Delimiter)
                .Trim(Constants.Delimiter.ToCharArray());
        }

        public static string NormalizeFolderPath(string path)
        {
            return string.Format("{0}{1}", NormalizePath(path), Constants.Delimiter);
        }

    }
}