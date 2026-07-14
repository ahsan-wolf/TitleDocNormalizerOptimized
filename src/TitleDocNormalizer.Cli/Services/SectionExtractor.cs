using System.Text;
using TitleDocNormalizer.Cli.Models;

namespace TitleDocNormalizer.Cli.Services;

public sealed class SectionExtractor
{
    public IReadOnlyList<DocumentSection> ExtractSections(IReadOnlyList<ClassifiedPage> pages)
    {
        var grouped = pages
            .Where(p => p.Kind is not PageKind.Miscellaneous and not PageKind.Unknown)
            .GroupBy(p => p.Kind)
            .OrderBy(g => g.Min(p => p.PageNumber));

        var sections = new List<DocumentSection>();
        foreach (var group in grouped)
        {
            var ordered = group.OrderBy(p => p.PageNumber).ToList();
            var name = ToSectionName(group.Key);
            var sb = new StringBuilder();
            foreach (var page in ordered)
            {
                sb.AppendLine($"\n<!-- Page {page.PageNumber}, Kind: {page.Kind}, Confidence: {page.Confidence:0.00} -->");
                sb.AppendLine(page.Text);
            }

            sections.Add(new DocumentSection(name, group.Key, ordered.Select(p => p.PageNumber).ToList(), sb.ToString().Trim()));
        }

        return sections;
    }

    public static string ToSectionName(PageKind kind) => kind switch
    {
        PageKind.Cover => "cover",
        PageKind.ScheduleA => "schedule_a",
        PageKind.ScheduleB => "schedule_b",
        PageKind.LegalDescription => "legal_description",
        PageKind.Exceptions => "exceptions",
        PageKind.Endorsements => "endorsements",
        PageKind.Conditions => "conditions",
        PageKind.Signature => "signature",
        _ => "miscellaneous"
    };
}
