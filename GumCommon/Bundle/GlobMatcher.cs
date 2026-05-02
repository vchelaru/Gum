using System.Text;
using System.Text.RegularExpressions;

namespace Gum.Bundle;

internal static class GlobMatcher
{
    public static Regex Compile(string searchPattern)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append('^');
        foreach (char c in searchPattern)
        {
            switch (c)
            {
                case '*':
                    builder.Append("[^/]*");
                    break;
                case '?':
                    builder.Append("[^/]");
                    break;
                default:
                    builder.Append(Regex.Escape(c.ToString()));
                    break;
            }
        }
        builder.Append('$');
        return new Regex(builder.ToString(), RegexOptions.CultureInvariant);
    }

    public static bool PatternHasPathSeparator(string searchPattern)
    {
        return searchPattern.IndexOf('/') >= 0 || searchPattern.IndexOf('\\') >= 0;
    }
}
