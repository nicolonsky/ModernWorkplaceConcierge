using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace ModernWorkplaceConcierge.Helpers
{
    public class FilenameHelper
    {
        public static string ProcessFileName (string input)
        {
            Regex illegalInFileName = new Regex(@"[\\/:*?""<>|]");
            
            return illegalInFileName.Replace(input, "");
        }
    }
}