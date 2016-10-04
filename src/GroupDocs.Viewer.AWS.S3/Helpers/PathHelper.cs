using System.IO;
using System.Text.RegularExpressions;

namespace GroupDocs.Viewer.AWS.S3.Helpers
{
    internal static class PathHelper
    {
        /// <summary>
        /// Converts guid to relative directory name.
        /// </summary>
        /// <param name="guid">The guid.</param>
        /// <returns>Relative directory name.</returns>
        public static string ToRelativeDirectoryName(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return string.Empty;

            string result = guid;

            const char replacementCharacter = '_';

            if (Path.IsPathRooted(result))
            {
                string root = Path.GetPathRoot(result);
                if (root.Equals(@"\"))
                    result = result.Substring(root.Length);

                if (root.Contains(":"))
                    result = result.Replace(':', replacementCharacter).Replace('\\', replacementCharacter).Replace('/', replacementCharacter);
            }

            result = Regex.Replace(result, "[_]{2,}", new string(replacementCharacter, 1));
            result = result.TrimStart(replacementCharacter);

            return result.Replace('.', replacementCharacter);
        }

        /// <summary>
        /// Replaces double slashes with path delimiter '/'
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Normalized path.</returns>
        public static string NormalizePath(string path)
        {
            return Regex.Replace(path, @"\\+", Constants.Delimiter)
                .Trim(Constants.Delimiter.ToCharArray());
        }
    }
}