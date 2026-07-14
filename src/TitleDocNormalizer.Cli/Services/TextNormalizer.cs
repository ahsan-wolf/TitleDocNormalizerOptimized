using System.Text;
using System.Text.RegularExpressions;
using TitleDocNormalizer.Cli.Models;

namespace TitleDocNormalizer.Cli.Services;

public sealed partial class TextNormalizer
{
    public NormalizedPage NormalizePage(RawPage page, ISet<string> repeatedLines)
    {
        var text = page.Text.Replace("\r\n", "\n").Replace('\r', '\n');
        text = ControlCharsRegex().Replace(text, " ");
        text = HyphenLineBreakRegex().Replace(text, "");
        text = MultiSpaceRegex().Replace(text, " ");

        var lines = text.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .Where(l => !repeatedLines.Contains(NormalizeLineKey(l)))
            .Where(l => !IsBoilerplate(l))
            .ToList();

        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            var normalized = NormalizeKnownLabels(line);
            if (normalized.Length > 0)
            {
                sb.AppendLine(normalized);
            }
        }

        var result = MultiBlankLineRegex().Replace(sb.ToString().Trim(), "\n\n");
        return new NormalizedPage(page.PageNumber, result, page.Text.Length, result.Length);
    }

    public static string NormalizeLineKey(string line)
    {
        return MultiSpaceRegex().Replace(line.Trim().ToLowerInvariant(), " ");
    }

    private static bool IsBoilerplate(string line)
    {
        var l = line.ToLowerInvariant();
        if (l.Length < 3) return true;
        if (PageNumberRegex().IsMatch(l)) return true;
        if (l.Contains("this page intentionally left blank")) return true;
        if (l.Contains("copyright") && l.Length < 120) return true;
        if (l.Contains("all rights reserved") && l.Length < 120) return true;
        if (l.Contains("privacy notice") && l.Length < 120) return true;
        return false;
    }

    private static string NormalizeKnownLabels(string line)
    {
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["policy no."] = "Policy Number:",
            ["policy number"] = "Policy Number:",
            ["file no."] = "File Number:",
            ["order no."] = "Order Number:",
            ["commitment no."] = "Commitment Number:",
            ["effective date"] = "Effective Date:",
            ["date of policy"] = "Policy Date:",
            ["amount of insurance"] = "Amount of Insurance:",
            ["insured"] = "Insured:",
            ["property address"] = "Property Address:",
            ["legal description"] = "Legal Description:"
        };

        foreach (var kvp in replacements)
        {
            line = Regex.Replace(line, $"^{Regex.Escape(kvp.Key)}\\s*[:#-]?", kvp.Value, RegexOptions.IgnoreCase);
        }

        return line.Trim();
    }

    [GeneratedRegex("[\\u0000-\\u0008\\u000B\\u000C\\u000E-\\u001F]")]
    private static partial Regex ControlCharsRegex();

    [GeneratedRegex("-\\s*\\n\\s*")]
    private static partial Regex HyphenLineBreakRegex();

    [GeneratedRegex("[ \\t]{2,}")]
    private static partial Regex MultiSpaceRegex();

    [GeneratedRegex("\\n{3,}")]
    private static partial Regex MultiBlankLineRegex();

    [GeneratedRegex("^(page|pg)\\s*\\d+\\s*(of\\s*\\d+)?$|^\\d+\\s*/\\s*\\d+$", RegexOptions.IgnoreCase)]
    private static partial Regex PageNumberRegex();
}
