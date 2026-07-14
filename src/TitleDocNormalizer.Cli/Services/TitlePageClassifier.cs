using System.Text.RegularExpressions;
using TitleDocNormalizer.Cli.Models;

namespace TitleDocNormalizer.Cli.Services;

public sealed partial class TitlePageClassifier
{
    public ClassifiedPage Classify(NormalizedPage page)
    {
        var text = page.Text;
        var lower = text.ToLowerInvariant();
        var signals = new List<string>();
        var scores = new Dictionary<PageKind, double>();

        void Add(PageKind kind, double score, string signal)
        {
            scores[kind] = scores.GetValueOrDefault(kind) + score;
            signals.Add(signal);
        }

        if (ContainsAny(lower, "schedule a", "schedule-a")) Add(PageKind.ScheduleA, 5, "schedule a");
        if (ContainsAny(lower, "schedule b", "schedule-b")) Add(PageKind.ScheduleB, 5, "schedule b");
        if (ContainsAny(lower, "legal description", "being more particularly described", "described property")) Add(PageKind.LegalDescription, 7, "legal description");
        if (ContainsAny(lower, "exceptions from coverage", "exceptions", "encumbrances", "easements", "restrictions")) Add(PageKind.Exceptions, 4, "exceptions/encumbrances");
        if (ContainsAny(lower, "endorsement", "endorsements")) Add(PageKind.Endorsements, 4, "endorsement");
        if (ContainsAny(lower, "conditions", "stipulations", "exclusions from coverage")) Add(PageKind.Conditions, 3, "conditions/exclusions");
        if (ContainsAny(lower, "policy number", "policy no", "amount of insurance", "insured", "property address")) Add(PageKind.Cover, 3, "cover identifiers");
        if (ContainsAny(lower, "signature", "authorized signatory", "countersigned")) Add(PageKind.Signature, 3, "signature");

        if (FieldLikeRegex().Matches(text).Count >= 5) Add(PageKind.Cover, 2, "many key-value labels");
        if (LegalBoundaryRegex().Matches(text).Count >= 3) Add(PageKind.LegalDescription, 3, "legal boundary vocabulary");

        if (scores.Count == 0)
        {
            return new ClassifiedPage(page.PageNumber, PageKind.Miscellaneous, 0.10, ["no strong signal"], text);
        }

        var winner = scores.OrderByDescending(kvp => kvp.Value).First();
        var confidence = Math.Min(0.99, 0.25 + winner.Value / 12.0);
        return new ClassifiedPage(page.PageNumber, winner.Key, confidence, signals.Distinct().ToArray(), text);
    }

    private static bool ContainsAny(string source, params string[] values) => values.Any(source.Contains);

    [GeneratedRegex("(?im)^\\s*[A-Za-z][A-Za-z /#.-]{2,40}\\s*:")]
    private static partial Regex FieldLikeRegex();

    [GeneratedRegex("(?i)\\b(lot|block|tract|subdivision|plat|survey|metes|bounds|north|south|east|west|county|recorded|book|page)\\b")]
    private static partial Regex LegalBoundaryRegex();
}
