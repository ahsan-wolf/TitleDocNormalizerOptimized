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
        string filePrefix,
        IReadOnlyList<RawPage> rawPages,
        IReadOnlyList<NormalizedPage> normalizedPages,
        IReadOnlyList<ClassifiedPage> classifiedPages,
        IReadOnlyList<DocumentSection> sections,
        IReadOnlyList<FieldRoute> routes,
        IReadOnlyList<RedactionMapping> redactionMappings,
        NormalizationManifest manifest,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputFolder);
        var pagesDir = Path.Combine(outputFolder, "pages");
        var sectionsDir = Path.Combine(outputFolder, "sections");
        var chunksDir = Path.Combine(outputFolder, "chunks");
        Directory.CreateDirectory(pagesDir);
        Directory.CreateDirectory(sectionsDir);
        Directory.CreateDirectory(chunksDir);

        string Prefixed(string name) => string.IsNullOrEmpty(filePrefix) ? name : $"{filePrefix}_{name}";

        await File.WriteAllTextAsync(Path.Combine(outputFolder, Prefixed("manifest.json")), JsonSerializer.Serialize(manifest, JsonOptions), cancellationToken);

        var document = new StringBuilder();
        foreach (var page in classifiedPages.OrderBy(p => p.PageNumber))
        {
            var pageFile = Path.Combine(pagesDir, Prefixed($"page_{page.PageNumber:000}.md"));
            var pageContent = $"# Page {page.PageNumber}\n\nKind: {page.Kind}\nConfidence: {page.Confidence:0.00}\nSignals: {string.Join(", ", page.Signals)}\n\n---\n\n{page.Text}\n";
            await File.WriteAllTextAsync(pageFile, pageContent, cancellationToken);

            document.AppendLine($"\n# Page {page.PageNumber} [{page.Kind}] confidence={page.Confidence:0.00}");
            document.AppendLine(page.Text);
        }

        await File.WriteAllTextAsync(Path.Combine(outputFolder, Prefixed("normalized_document.md")), document.ToString().Trim(), cancellationToken);

        foreach (var section in sections)
        {
            var path = Path.Combine(sectionsDir, Prefixed($"{section.Name}.md"));
            await File.WriteAllTextAsync(path, $"# {section.Name}\n\nPages: {string.Join(", ", section.PageNumbers)}\n\n{section.Text}\n", cancellationToken);
        }

        var routedAll = new StringBuilder();
        foreach (var route in routes)
        {
            var path = Path.Combine(chunksDir, Prefixed($"{route.FieldName}.md"));
            await File.WriteAllTextAsync(path, route.RoutedText, cancellationToken);
            routedAll.AppendLine($"\n# Field route: {route.FieldName}");
            routedAll.AppendLine(route.RoutedText);
        }

        await File.WriteAllTextAsync(Path.Combine(outputFolder, Prefixed("routed_llm_input.md")), routedAll.ToString().Trim(), cancellationToken);

        await File.WriteAllTextAsync(Path.Combine(outputFolder, Prefixed("field_mapping.json")), JsonSerializer.Serialize(redactionMappings, JsonOptions), cancellationToken);
    }
}
