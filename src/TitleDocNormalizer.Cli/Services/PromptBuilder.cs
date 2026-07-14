using System.Text;
using TitleDocNormalizer.Cli.Models;

namespace TitleDocNormalizer.Cli.Services;

public sealed class PromptBuilder
{
    public string BuildStrictJsonPrompt(IReadOnlyList<FieldRoute> routes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are extracting fields from normalized title-industry policy documents.");
        sb.AppendLine("Use only the provided routed text. Do not guess. If a field is not present, return null.");
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
        sb.AppendLine("Routed source text:");

        foreach (var route in routes)
        {
            sb.AppendLine($"\n--- FIELD ROUTE: {route.FieldName} ---");
            sb.AppendLine($"Candidate pages: {string.Join(", ", route.PageNumbers)}");
            sb.AppendLine(route.RoutedText);
        }

        return sb.ToString();
    }
}
