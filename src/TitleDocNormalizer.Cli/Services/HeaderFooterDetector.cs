using TitleDocNormalizer.Cli.Models;

namespace TitleDocNormalizer.Cli.Services;

public sealed class HeaderFooterDetector
{
    public ISet<string> DetectRepeatedLines(IReadOnlyList<RawPage> pages)
    {
        if (pages.Count < 3) return new HashSet<string>();

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var page in pages)
        {
            var lines = page.Text.Replace("\r\n", "\n").Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length is >= 4 and <= 120)
                .Select(TextNormalizer.NormalizeLineKey)
                .Distinct();

            foreach (var line in lines)
            {
                counts[line] = counts.GetValueOrDefault(line) + 1;
            }
        }

        var threshold = Math.Max(3, (int)Math.Ceiling(pages.Count * 0.40));
        return counts
            .Where(kvp => kvp.Value >= threshold)
            .Select(kvp => kvp.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
