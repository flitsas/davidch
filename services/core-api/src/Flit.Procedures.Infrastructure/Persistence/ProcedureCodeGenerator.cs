using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Flit.Procedures.Infrastructure.Persistence;

public static partial class ProcedureCodeGenerator
{
    public static string FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        var normalized = name.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToUpperInvariant(ch));
            }
            else if (char.IsWhiteSpace(ch) || ch is '-' or '_' or '/')
            {
                builder.Append('_');
            }
        }

        var code = CollapseUnderscores().Replace(builder.ToString(), "_").Trim('_');
        if (string.IsNullOrEmpty(code))
        {
            throw new ArgumentException("Name must contain at least one letter or digit.", nameof(name));
        }

        return code;
    }
}

partial class ProcedureCodeGenerator
{
    [GeneratedRegex("_+")]
    private static partial Regex CollapseUnderscores();
}
