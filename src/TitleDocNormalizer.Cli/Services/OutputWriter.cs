using System.Text;
using System.Text.Json;
using TitleDocNormalizer.Cli.Models;

namespace TitleDocNormalizer.Cli.Services;

public sealed class OutputWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public async Task WriteAsync(
        string outputFolder,
        IReadOnlyList<RawPage> rawPages,
        IReadOnlyList<NormalizedPage> normalizedPages,
        IReadOnlyList<ClassifiedPage> classifiedPages,
        IReadOnlyList<DocumentSection> sections,
        IReadOnlyList<FieldRoute> routes,
        NormalizationManifest manifest,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputFolder);
        Directory.CreateDirectory(Path.Combine(outputFolder, "pages"));
        Directory.CreateDirectory(Path.Combine(outputFolder, "sections"));
        Directory.CreateDirectory(Path.Combine(outputFolder, "chunks"));

        await File.WriteAllTextAsync(Path.Combine(outputFolder, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken);

        var document = new StringBuilder();
        foreach (var page in classifiedPages.OrderBy(p => p.PageNumber))
        {
            var pageFile = Path.Combine(outputFolder, "pages", $"page_{page.PageNumber:000}.md");
            var pageContent = $"# Page {page.PageNumber}\n\nKind: {page.Kind}\nConfidence: {page.Confidence:0.00}\nSignals: {string.Join(", ", page.Signals)}\n\n---\n\n{page.Text}\n";
            await File.WriteAllTextAsync(pageFile, pageContent, cancellationToken);

            document.AppendLine($"\n# Page {page.PageNumber} [{page.Kind}] confidence={page.Confidence:0.00}");
            document.AppendLine(page.Text);
        }

        await File.WriteAllTextAsync(Path.Combine(outputFolder, "normalized_document.md"), document.ToString().Trim(), cancellationToken);

        foreach (var section in sections)
        {
            var path = Path.Combine(outputFolder, "sections", $"{section.Name}.md");
            await File.WriteAllTextAsync(path, $"# {section.Name}\n\nPages: {string.Join(", ", section.PageNumbers)}\n\n{section.Text}\n", cancellationToken);
        }

        var routedAll = new StringBuilder();
        foreach (var route in routes)
        {
            var path = Path.Combine(outputFolder, "chunks", $"{route.FieldName}.md");
            await File.WriteAllTextAsync(path, route.RoutedText, cancellationToken);
            routedAll.AppendLine($"\n# Field route: {route.FieldName}");
            routedAll.AppendLine(route.RoutedText);
        }

        await File.WriteAllTextAsync(Path.Combine(outputFolder, "routed_llm_input.md"), routedAll.ToString().Trim(), cancellationToken);
    }
}
