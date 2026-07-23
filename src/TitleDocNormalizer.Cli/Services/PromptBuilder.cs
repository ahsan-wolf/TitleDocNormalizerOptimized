using System.Text;
using TitleDocNormalizer.Cli.Models;

namespace TitleDocNormalizer.Cli.Services;

public sealed class PromptBuilder
{
    public string BuildStrictJsonPrompt(IReadOnlyList<ClassifiedPage> pages, IReadOnlyList<FieldRoute> routes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are extracting fields from normalized title-industry policy documents.");
        sb.AppendLine("Use only the provided source pages. Do not guess. If a field is not present, return null.");
        sb.AppendLine("Return strict JSON only. No markdown. No explanation.");
        sb.AppendLine();
        sb.AppendLine("Required JSON schema:");
        sb.AppendLine("{");
        foreach (var route in routes)
        {
            sb.AppendLine($"  \"{route.FieldName}\": {{ \"value\": null, \"confidence\": 0.0, \"source_pages\": [] }},");
        }
        sb.AppendLine("  \"warnings\": []");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Field candidate pages:");
        foreach (var route in routes)
        {
            sb.AppendLine($"- {route.FieldName}: pages {string.Join(", ", route.PageNumbers)}");
        }

        sb.AppendLine();
        sb.AppendLine("Source pages (each page listed once, even if used by multiple fields):");

        var pageLookup = pages.ToDictionary(p => p.PageNumber);
        var referencedPageNumbers = routes
            .SelectMany(r => r.PageNumbers)
            .Distinct()
            .Order();

        foreach (var pageNumber in referencedPageNumbers)
        {
            if (!pageLookup.TryGetValue(pageNumber, out var page)) continue;
            sb.AppendLine($"\n--- PAGE {pageNumber} [{page.Kind}] ---");
            sb.AppendLine(page.Text);
        }

        return sb.ToString();
    }
}
