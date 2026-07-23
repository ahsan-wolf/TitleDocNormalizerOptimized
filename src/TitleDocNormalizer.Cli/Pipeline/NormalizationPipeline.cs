using TitleDocNormalizer.Cli.Models;
using TitleDocNormalizer.Cli.Services;

namespace TitleDocNormalizer.Cli.Pipeline;

public sealed class NormalizationPipeline(
    IPageTextExtractor extractor,
    TextNormalizer normalizer,
    HeaderFooterDetector headerFooterDetector,
    TitlePageClassifier classifier,
    SectionExtractor sectionExtractor,
    FieldRouter fieldRouter,
    ConfidentialFieldRedactor redactor,
    PromptBuilder promptBuilder,
    OutputWriter outputWriter)
{
    public async Task<PipelineResult> RunAsync(CliOptions options, CancellationToken cancellationToken = default)
    {
        var rawPages = await extractor.ExtractAsync(options.InputPdf!, cancellationToken);
        var repeatedLines = headerFooterDetector.DetectRepeatedLines(rawPages);

        var normalizedPages = rawPages
            .AsParallel()
            .AsOrdered()
            .Select(p => normalizer.NormalizePage(p, repeatedLines))
            .ToList();

        var classifiedPages = normalizedPages
            .AsParallel()
            .AsOrdered()
            .Select(classifier.Classify)
            .ToList();

        var sections = sectionExtractor.ExtractSections(classifiedPages);
        var routes = fieldRouter.BuildRoutes(classifiedPages, sections, options.MaxPagesPerField);

        var redacted = redactor.Redact(classifiedPages, sections, routes);
        classifiedPages = redacted.Pages.ToList();
        sections = redacted.Sections.ToList();
        routes = redacted.Routes.ToList();

        var manifest = new NormalizationManifest(
            SourceFile: Path.GetFullPath(options.InputPdf!),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            Pages: classifiedPages.Select(p => new ManifestPage(
                p.PageNumber,
                p.Kind,
                p.Confidence,
                rawPages[p.PageNumber - 1].Text.Length,
                p.Text.Length,
                p.Signals)).ToList(),
            Sections: sections.Select(s => s.Name).ToList(),
            FieldRoutes: routes.Select(r => r.FieldName).ToList(),
            Notes: "Optimized title-document normalization with page classification, section extraction, field-aware routing, and confidential-field redaction.");

        var filePrefix = BuildFilePrefix(options.InputPdf!);

        await outputWriter.WriteAsync(options.OutputFolder, filePrefix, rawPages, normalizedPages, classifiedPages, sections, routes, redacted.Mappings, manifest, cancellationToken);

        if (options.GeneratePrompt)
        {
            var prompt = promptBuilder.BuildStrictJsonPrompt(classifiedPages, routes);
            var promptDir = Path.Combine(options.OutputFolder, "prompts");
            Directory.CreateDirectory(promptDir);
            await File.WriteAllTextAsync(Path.Combine(promptDir, $"{filePrefix}_field_extraction_prompt.md"), prompt, cancellationToken);
        }

        return new PipelineResult(manifest, routes);
    }

    private static string BuildFilePrefix(string inputPdf)
    {
        var name = Path.GetFileNameWithoutExtension(inputPdf);
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
