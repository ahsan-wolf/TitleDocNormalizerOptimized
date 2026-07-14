using System.Text;
using System.Text.RegularExpressions;
using TitleDocNormalizer.Cli.Models;

namespace TitleDocNormalizer.Cli.Services;

public sealed partial class FieldRouter
{
    private static readonly IReadOnlyDictionary<string, FieldProfile> Profiles = new Dictionary<string, FieldProfile>(StringComparer.OrdinalIgnoreCase)
    {
        ["policy_number"] = new("Policy Number", [PageKind.Cover, PageKind.ScheduleA], ["policy number", "policy no", "policy #", "commitment number"]),
        ["effective_date"] = new("Effective Date", [PageKind.Cover, PageKind.ScheduleA], ["effective date", "date of policy", "policy date"]),
        ["insured_name"] = new("Insured Name", [PageKind.Cover, PageKind.ScheduleA], ["insured", "name of insured", "proposed insured"]),
        ["property_address"] = new("Property Address", [PageKind.Cover, PageKind.ScheduleA], ["property address", "premises", "land referred to", "street address"]),
        ["county"] = new("County", [PageKind.Cover, PageKind.ScheduleA, PageKind.LegalDescription], ["county", "state of"]),
        ["state"] = new("State", [PageKind.Cover, PageKind.ScheduleA, PageKind.LegalDescription], ["state", "state of"]),
        ["apn_or_parcel"] = new("APN or Parcel ID", [PageKind.Cover, PageKind.ScheduleA, PageKind.LegalDescription], ["apn", "parcel", "tax id", "assessor"]),
        ["amount_of_insurance"] = new("Amount of Insurance", [PageKind.Cover, PageKind.ScheduleA], ["amount of insurance", "liability amount", "policy amount"]),
        ["legal_description"] = new("Legal Description", [PageKind.LegalDescription, PageKind.ScheduleA], ["legal description", "lot", "block", "subdivision", "metes", "bounds"]),
        ["exceptions"] = new("Exceptions", [PageKind.Exceptions, PageKind.ScheduleB], ["exceptions", "schedule b", "easement", "restriction", "encumbrance"])
    };

    public IReadOnlyList<FieldRoute> BuildRoutes(IReadOnlyList<ClassifiedPage> pages, IReadOnlyList<DocumentSection> sections, int maxPagesPerField)
    {
        var routes = new List<FieldRoute>();
        foreach (var profile in Profiles)
        {
            var scoredPages = pages.Select(p => new
                {
                    Page = p,
                    Score = ScorePage(p, profile.Value)
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Page.PageNumber)
                .Take(Math.Max(1, maxPagesPerField))
                .ToList();

            if (scoredPages.Count == 0)
            {
                scoredPages = pages.OrderBy(p => p.PageNumber).Take(Math.Min(maxPagesPerField, pages.Count)).Select(p => new { Page = p, Score = 0.01 }).ToList();
            }

            var selectedPageNumbers = scoredPages.Select(x => x.Page.PageNumber).Distinct().Order().ToList();
            var relatedSections = sections
                .Where(s => s.PageNumbers.Any(selectedPageNumbers.Contains))
                .Select(s => s.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"# Routed input for {profile.Value.DisplayName}");
            foreach (var x in scoredPages)
            {
                sb.AppendLine($"\n## Page {x.Page.PageNumber} [{x.Page.Kind}] score={x.Score:0.00}");
                sb.AppendLine(x.Page.Text);
            }

            routes.Add(new FieldRoute(profile.Key, selectedPageNumbers, relatedSections, scoredPages.Sum(x => x.Score), sb.ToString().Trim()));
        }

        return routes;
    }

    private static double ScorePage(ClassifiedPage page, FieldProfile profile)
    {
        double score = 0;
        if (profile.PreferredKinds.Contains(page.Kind)) score += 5 * page.Confidence;

        var lower = page.Text.ToLowerInvariant();
        foreach (var keyword in profile.Keywords)
        {
            if (lower.Contains(keyword, StringComparison.OrdinalIgnoreCase)) score += 2;
        }

        if (profile.DisplayName.Contains("Legal", StringComparison.OrdinalIgnoreCase) && LegalBoundaryRegex().IsMatch(page.Text)) score += 3;
        if (profile.DisplayName.Contains("Address", StringComparison.OrdinalIgnoreCase) && AddressLikeRegex().IsMatch(page.Text)) score += 2;
        if (profile.DisplayName.Contains("Amount", StringComparison.OrdinalIgnoreCase) && MoneyRegex().IsMatch(page.Text)) score += 2;
        if (profile.DisplayName.Contains("Date", StringComparison.OrdinalIgnoreCase) && DateRegex().IsMatch(page.Text)) score += 2;

        return score;
    }

    private sealed record FieldProfile(string DisplayName, PageKind[] PreferredKinds, string[] Keywords);

    [GeneratedRegex("(?i)\\b(lot|block|tract|subdivision|plat|survey|metes|bounds|north|south|east|west|recorded|book|page)\\b")]
    private static partial Regex LegalBoundaryRegex();

    [GeneratedRegex("(?i)\\b(street|st\\.|avenue|ave\\.|road|rd\\.|drive|dr\\.|lane|ln\\.|boulevard|blvd\\.|suite|unit)\\b")]
    private static partial Regex AddressLikeRegex();

    [GeneratedRegex("\\$\\s?\\d{1,3}(,\\d{3})*(\\.\\d{2})?")]
    private static partial Regex MoneyRegex();

    [GeneratedRegex("(?i)\\b(\\d{1,2}/\\d{1,2}/\\d{2,4}|january|february|march|april|may|june|july|august|september|october|november|december)\\b")]
    private static partial Regex DateRegex();
}
