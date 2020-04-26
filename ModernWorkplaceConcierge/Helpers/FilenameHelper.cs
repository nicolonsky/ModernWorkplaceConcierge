using System.Text.RegularExpressions;

namespace ModernWorkplaceConcierge.Helpers
{
    public class FilenameHelper
    {
        public static string ProcessFileName(string input)
        {
            Regex illegalInFileName = new Regex(@"[\\/:*?""<>|]");

            return illegalInFileName.Replace(input, "");
        }
    }
}