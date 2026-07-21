using System.Text.RegularExpressions;
using TitleDocNormalizer.Cli.Models;

namespace TitleDocNormalizer.Cli.Services;

public sealed partial class ConfidentialFieldRedactor
{
    private enum DummyKind { Money, Organization }

    private sealed record FieldPattern(string FieldName, Regex Pattern, DummyKind Kind);

    private static readonly string[] MoneyDummyPool =
    [
        "$250,000.00", "$500,000.00", "$750,000.00", "$1,000,000.00", "$1,250,000.00", "$1,500,000.00"
    ];

    private static readonly string[] LenderDummyPool =
    [
        "Sample Lending Bank, N.A.", "Example Mortgage Corp.", "Placeholder Federal Credit Union"
    ];

    private static readonly string[] UnderwriterDummyPool =
    [
        "Sample Title Insurance Company", "Placeholder National Title Insurance Co.", "Example Title Underwriters Inc."
    ];

    private readonly List<FieldPattern> _patterns =
    [
        new("policy_amount", PolicyAmountRegex(), DummyKind.Money),
        new("insurance_amount", InsuranceAmountRegex(), DummyKind.Money),
        new("premium", PremiumRegex(), DummyKind.Money),
        new("lender", LenderRegex(), DummyKind.Organization),
        new("underwriter", UnderwriterRegex(), DummyKind.Organization)
    ];

    public RedactionResult Redact(
        IReadOnlyList<ClassifiedPage> pages,
        IReadOnlyList<DocumentSection> sections,
        IReadOnlyList<FieldRoute> routes)
    {
        var dummyByKey = new Dictionary<(string FieldName, string Original), string>();
        var nextPoolIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var pageNumbersByKey = new Dictionary<(string FieldName, string Original), SortedSet<int>>();

        var redactedPages = pages
            .Select(p => p with { Text = RedactText(p.Text, p.PageNumber, dummyByKey, nextPoolIndex, pageNumbersByKey) })
            .ToList();

        var redactedSections = sections
            .Select(s => s with { Text = RedactText(s.Text, null, dummyByKey, nextPoolIndex, pageNumbersByKey) })
            .ToList();

        var redactedRoutes = routes
            .Select(r => r with { RoutedText = RedactText(r.RoutedText, null, dummyByKey, nextPoolIndex, pageNumbersByKey) })
            .ToList();

        var mappings = dummyByKey
            .Select(kv => new RedactionMapping(
                kv.Key.FieldName,
                kv.Key.Original,
                kv.Value,
                pageNumbersByKey.TryGetValue(kv.Key, out var pageNumbers) ? pageNumbers.ToList() : []))
            .OrderBy(m => m.FieldName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.OriginalValue, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new RedactionResult(redactedPages, redactedSections, redactedRoutes, mappings);
    }

    private string RedactText(
        string text,
        int? pageNumber,
        Dictionary<(string FieldName, string Original), string> dummyByKey,
        Dictionary<string, int> nextPoolIndex,
        Dictionary<(string FieldName, string Original), SortedSet<int>> pageNumbersByKey)
    {
        foreach (var pattern in _patterns)
        {
            text = pattern.Pattern.Replace(text, match =>
            {
                var valueGroup = match.Groups["value"];
                var original = valueGroup.Value.Trim();
                if (original.Length == 0) return match.Value;

                var key = (pattern.FieldName, original);
                if (!dummyByKey.TryGetValue(key, out var dummy))
                {
                    var index = nextPoolIndex.TryGetValue(pattern.FieldName, out var i) ? i : 0;
                    var pool = pattern.Kind == DummyKind.Money
                        ? MoneyDummyPool
                        : pattern.FieldName == "lender" ? LenderDummyPool : UnderwriterDummyPool;
                    dummy = pool[index % pool.Length];
                    nextPoolIndex[pattern.FieldName] = index + 1;
                    dummyByKey[key] = dummy;
                }

                if (pageNumber is int pn)
                {
                    if (!pageNumbersByKey.TryGetValue(key, out var pageSet))
                    {
                        pageSet = [];
                        pageNumbersByKey[key] = pageSet;
                    }
                    pageSet.Add(pn);
                }

                var relativeStart = valueGroup.Index - match.Index;
                var prefix = match.Value[..relativeStart];
                var suffix = match.Value[(relativeStart + valueGroup.Length)..];
                return prefix + dummy + suffix;
            });
        }

        return text;
    }

    [GeneratedRegex(@"\bpolicy\s*amount\s*[:\-]\s*(?<value>\$[\d,]+(?:\.\d{2})?)", RegexOptions.IgnoreCase)]
    private static partial Regex PolicyAmountRegex();

    [GeneratedRegex(@"\b(?:amount\s*of\s*insurance|liability\s*amount)\s*[:\-]\s*(?<value>\$[\d,]+(?:\.\d{2})?)", RegexOptions.IgnoreCase)]
    private static partial Regex InsuranceAmountRegex();

    [GeneratedRegex(@"\b(?:total\s*premium|premium)\s*[:\-]\s*(?<value>\$[\d,]+(?:\.\d{2})?)", RegexOptions.IgnoreCase)]
    private static partial Regex PremiumRegex();

    [GeneratedRegex(@"\b(?:lender|mortgagee)\s*[:\-]\s*(?<value>[^\r\n]+?)(?=\s{2,}|[\r\n]|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex LenderRegex();

    [GeneratedRegex(@"\b(?:underwriter|underwritten\s*by)\s*[:\-]\s*(?<value>[^\r\n]+?)(?=\s{2,}|[\r\n]|$)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex UnderwriterRegex();
}
