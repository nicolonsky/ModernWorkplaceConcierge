/*
    Highlight 7.1 for NuGet
    http://softwaremaniacs.org/soft/highlight/en/
    
    == Content ==

    ~/
        App_Code/
            Highlight/
                Highlight.cs
                Lisence Files
        Content/
            Highlight/
                Theme Skins (css)
        Scripts/
            Highlight/
                hilight.pack.js
    
    packaged by @daruyanagi. http://daruyanagi.net/
*/

using System.Web;

public static class Highlight
{
    const string STYLE_DIR = "~/Content/Highlight/";
    const string SCRIPT_DIR = "~/Scripts/Highlight/";
    const string HTML = @"<!-- for Highlight.js support -->
        <link rel=""stylesheet"" href=""{0}{1}.css"">
        <script src=""{2}highlight.pack.js""></script>
        <script>hljs.initHighlightingOnLoad();</script>";
    
    public static HtmlString Include(string theme = "default")
    {
        return new HtmlString(
            string.Format(
                HTML, 
                VirtualPathUtility.ToAbsolute(STYLE_DIR),
                theme,
                VirtualPathUtility.ToAbsolute(SCRIPT_DIR)
            )
        );
    }
}
