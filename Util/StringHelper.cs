using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Tacoly.Util;

public static partial class StringHelper
{
    public static string Tabbed(this string str, int tabs = 1)
    {
        return str.TrimEnd().Split('\n').Select(c => new string('\t', tabs) + c).Aggregate((a, b) => a + '\n' + b);
    }

    public static string Tabbed(this StringBuilder sb, int tabs = 1)
    {
        return sb.ToString().Tabbed(tabs);
    }

    public static StringBuilder MaybeAppendLine(this StringBuilder sb, string other)
    {
        if (!String.IsNullOrWhiteSpace(other)) sb.AppendLine(other.TrimEnd());
        return sb;
    }

    public static StringBuilder MaybeAppendLine(this StringBuilder sb, StringBuilder other)
    {
        return sb.MaybeAppendLine(other.ToString());
    }

}