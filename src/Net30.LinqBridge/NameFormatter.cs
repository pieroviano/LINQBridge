using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace System;

internal static class NameFormatter
{
    private static readonly Regex WordRegex =
        new(@"[A-Z]+(?![a-z])|[A-Z][a-z]*|\d+",
            RegexOptions.Compiled);

    public static string FormatPascalName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var matches = WordRegex.Matches(value);
        if (matches.Count == 0)
        {
            return value;
        }

        var words = new List<string>();

        foreach (Match m in matches)
        {
            words.Add(m.Value);
        }

        var isPascalStyle = char.IsUpper(value[0]);

        var sb = new StringBuilder();

        for (var i = 0; i < words.Count; i++)
        {
            var w = words[i];

            if (i > 0 && isPascalStyle)
            {
                // Preserve acronyms (2+ uppercase letters)
                if (!(w.Length > 1 && IsAllUpper(w)))
                {
                    w = w.ToLowerInvariant();
                }
            }

            if (i > 0)
            {
                sb.Append(' ');
            }

            sb.Append(w);
        }

        return sb.ToString();
    }

    private static bool IsAllUpper(string s)
    {
        foreach (var c in s)
        {
            if (char.IsLetter(c) && !char.IsUpper(c))
            {
                return false;
            }
        }

        return true;
    }
}